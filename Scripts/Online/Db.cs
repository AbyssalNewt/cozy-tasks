using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
 
public partial class Db : Node
{
	private const string SupabaseUrl = "https://qyidofxlzkjzwmwclbze.supabase.co";
	private const string AnonKey = "sb_publishable_P-8rMFNR9mXS1jPoFfiW1g_pX9Hgqzd";
	private const string SessionFile = "user://session.cfg";
	
	public static Db Instance {get; private set;}

	//when user signs in or out
	[Signal] public delegate void SessionChangedEventHandler(bool loggedIn);
	/// The logged-in account.
	public Guid UserId { get; private set; } = Guid.Empty;
	/// Checking who is playing, the logged-in account, or a child they picked.
	public Guid PlayingAsId { get; private set; } = Guid.Empty;
	public bool IsLoggedIn => _accessToken != "";
 
	private string _accessToken = "";
	private string _refreshToken = "";
	private double _expiresAt;
 
	private static readonly System.Net.Http.HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
 
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,   // XpReward <-> xp_reward
		PropertyNameCaseInsensitive = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};
	private const string ProfileCols = "id,display_name,avatar,account_type,has_login,xp";
	private const string TaskSelect =
		"select=*,owner:profiles!tasks_owner_id_fkey(display_name),assignee:profiles!tasks_assigned_to_fkey(display_name)";
 
	public override void _EnterTree() => Instance = this;
	/// Creates an account. Data = true if the player is now logged in.
	public async Task<DbResult<bool>> SignUpAsync(string email, string password, string displayName, bool isChild = false)
	{
		var res = await Send<AuthSession>(HttpMethod.Post, "/auth/v1/signup", new
		{
			email,
			password,
			data = new { display_name = displayName, account_type = isChild ? "child" : "adult" },
		}, useAuth: false);
		if (!res.Ok) return Fail<bool>(res.Error);
 
		bool loggedIn = !string.IsNullOrEmpty(res.Data?.AccessToken);
		if (loggedIn) SetSession(res.Data);
		return new DbResult<bool> { Ok = true, Data = loggedIn };
	}
 
	public async Task<DbResult<bool>> LoginAsync(string email, string password)
	{
		var res = await Send<AuthSession>(HttpMethod.Post, "/auth/v1/token?grant_type=password",
			new { email, password }, useAuth: false);
		if (!res.Ok) return Fail<bool>(res.Error);
 
		SetSession(res.Data);
		return new DbResult<bool> { Ok = true, Data = true };
	}
 
	public async Task LogoutAsync()
	{
		if (IsLoggedIn)
			await Send<JsonElement>(HttpMethod.Post, "/auth/v1/logout", new { });
		ClearSession();
	}
 
	/// Call once at startup: logs back in automatically if a session was saved.
	public async Task<bool> RestoreSessionAsync()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(SessionFile) != Error.Ok) return false;
 
		_refreshToken = cfg.GetValue("auth", "refresh_token", "").AsString();
		if (_refreshToken == "") return false;
		return await RefreshAsync();
	}
 
	/// A child picked their name on the family device (option 1).
	public void PlayAs(Guid childProfileId) => PlayingAsId = childProfileId;
 
	public void PlayAsMe() => PlayingAsId = UserId;
	public async Task<DbResult<Profile>> GetMyProfileAsync() =>
		First(await Send<List<Profile>>(HttpMethod.Get, $"/rest/v1/profiles?id=eq.{UserId}&select=*"));
	/// Change your own name/avatar. Pass null to leave a value unchanged.
	public async Task<DbResult<Profile>> UpdateProfileAsync(string displayName = null, string avatar = null) =>
		First(await Send<List<Profile>>(HttpMethod.Patch, $"/rest/v1/profiles?id=eq.{UserId}",
			new { display_name = displayName, avatar }, prefer: "return=representation"));
	/// Adult adds a child to a household.
	public Task<DbResult<Profile>> CreateChildAsync(Guid householdId, string name, string avatar = null) =>
		Rpc<Profile>("create_child", new {hid = householdId, child_name = name, child_avatar = avatar }); 
	public Task<DbResult<Profile>> UpdateChildAsync(Guid childId, string name = null, string avatar = null) =>
		Rpc<Profile>("update_child", new {child_id = childId, child_name = name, child_avatar = avatar });
	public Task<DbResult<JsonElement>> DeleteChildAsync(Guid childId) =>
		Rpc<JsonElement>("delete_child", new {child_id = childId });

	/// Adults only. Makes you the admin. Data.InviteCode is the code to share.
	public Task<DbResult<Household>> CreateHouseholdAsync(string name) =>
		Rpc<Household>("create_household", new {hname = name });
 
	public Task<DbResult<Household>> JoinHouseholdAsync(string inviteCode) =>
		Rpc<Household>("join_household", new {code = inviteCode });
 
	public Task<DbResult<List<HouseholdMembership>>> GetMyHouseholdsAsync() =>
		Send<List<HouseholdMembership>>(HttpMethod.Get,
			$"/rest/v1/household_members?user_id=eq.{UserId}&select=role,household:households(id,name,invite_code)");
 
	/// Everyone in the home, including child profiles (use for "who is playing?").
	public Task<DbResult<List<HouseholdMember>>> GetHouseholdMembersAsync(Guid householdId) =>
		Send<List<HouseholdMember>>(HttpMethod.Get,
			$"/rest/v1/household_members?household_id=eq.{householdId}&select=role,user_id,profile:profiles({ProfileCols})");
 
	/// Household members sorted by XP, highest first.
	public async Task<DbResult<List<HouseholdMember>>> GetLeaderboardAsync(Guid householdId)
	{
		var res = await GetHouseholdMembersAsync(householdId);
		if (res.Ok)
			res.Data = res.Data.OrderByDescending(m => m.Profile?.Xp ?? 0).ToList();
		return res;
	}
 
	public Task<DbResult<JsonElement>> LeaveHouseholdAsync(Guid householdId) =>
		Send<JsonElement>(HttpMethod.Delete,
			$"/rest/v1/household_members?household_id=eq.{householdId}&user_id=eq.{UserId}");
	/// Send a request using their friend code (Profile.FriendCode).
	/// Data = "sent", or "accepted" if they had already sent you one.
	public Task<DbResult<string>> SendFriendRequestAsync(string friendCode) =>
		Rpc<string>("send_friend_request", new { code = friendCode });

	/// Your friends and pending requests (both directions).
	/// Use f.Other(Db.Instance.UserId) and f.IsIncomingRequest(Db.Instance.UserId).
	public Task<DbResult<List<Friendship>>> GetFriendshipsAsync() =>
		Send<List<Friendship>>(HttpMethod.Get, "/rest/v1/friendships?select=*,"
			+ $"requester:profiles!friendships_requester_fkey({ProfileCols}),"
			+ $"addressee:profiles!friendships_addressee_fkey({ProfileCols})"
			+ "&order=created_at.desc");
 
	public Task<DbResult<JsonElement>> RespondFriendRequestAsync(Guid friendshipId, bool accept) =>
		Rpc<JsonElement>("respond_friend_request", new { request_id = friendshipId, accept });
	/// Unfriend, or cancel a request you sent.
	public Task<DbResult<JsonElement>> RemoveFriendshipAsync(Guid friendshipId) =>
		Send<JsonElement>(HttpMethod.Delete, $"/rest/v1/friendships?id=eq.{friendshipId}");

	/// Every task in a household (shared house chores).
	public Task<DbResult<List<TaskRecord>>> GetHouseholdTasksAsync(Guid householdId) =>
		GetTasks($"household_id=eq.{householdId}");
	public Task<DbResult<List<TaskRecord>>> GetMyTasksAsync() =>
		GetTasks($"or=(owner_id.eq.{PlayingAsId},assigned_to.eq.{PlayingAsId})");
	public Task<DbResult<List<TaskRecord>>> GetFriendsTasksAsync() =>
		GetTasks($"visibility=eq.friends&owner_id=neq.{UserId}");
 
	public Task<DbResult<List<TaskRecord>>> GetPublicTasksAsync(int limit = 50) =>
		GetTasks($"visibility=eq.public&limit={limit}");
 
	/// Save a new task. Set HouseholdId for a house chore (adults only),
	/// Null for a personal task. OwnerId empty = whoever is playing.
	public async Task<DbResult<TaskRecord>> CreateTaskAsync(TaskRecord task)
	{
		var row = task.ToWrite(includeId: true);
		if (!row.ContainsKey("owner_id") && PlayingAsId != UserId)
			row["owner_id"] = PlayingAsId;
		return First(await Send<List<TaskRecord>>(HttpMethod.Post, "/rest/v1/tasks?" + TaskSelect, row,
			prefer: "return=representation"));
	}
 
	/// Saves all fields of the task (find it with task.Id).
	public async Task<DbResult<TaskRecord>> UpdateTaskAsync(TaskRecord task) =>
		First(await Send<List<TaskRecord>>(HttpMethod.Patch, $"/rest/v1/tasks?id=eq.{task.Id}&" + TaskSelect,
			task.ToWrite(includeId: false), prefer: "return=representation"));
 
	public Task<DbResult<JsonElement>> DeleteTaskAsync(Guid taskId) =>
		Send<JsonElement>(HttpMethod.Delete, $"/rest/v1/tasks?id=eq.{taskId}");
	public Task<DbResult<int>> CompleteTaskAsync(Guid taskId, DateTime date) =>
		Rpc<int>("complete_task", new { tid = taskId, on_date = DateString(date), as_profile = PlayingAsId });
 

	public Task<DbResult<JsonElement>> UndoCompletionAsync(Guid taskId, DateTime date) =>
		Rpc<JsonElement>("undo_completion", new { tid = taskId, on_date = DateString(date) });
 
	/// Which instances of these tasks are completed between two dates
	/// (use it to fill TaskManager's completed state for the month view).
	public Task<DbResult<List<TaskCompletion>>> GetCompletionsAsync(IEnumerable<Guid> taskIds, DateTime from, DateTime to)
	{
		string ids = string.Join(",", taskIds);
		if (ids == "")
			return Task.FromResult(new DbResult<List<TaskCompletion>> { Ok = true, Data = new() });
		return Send<List<TaskCompletion>>(HttpMethod.Get,
			$"/rest/v1/task_completions?task_id=in.({ids})"
			+ $"&occurrence_date=gte.{DateString(from)}&occurrence_date=lte.{DateString(to)}"
			+ "&select=task_id,occurrence_date,profile_id,xp_earned,completed_at");
	}
	/// Household activity feed: who completed what, newest first.
	public Task<DbResult<List<TaskCompletion>>> GetActivityAsync(Guid householdId, int limit = 20) =>
		Send<List<TaskCompletion>>(HttpMethod.Get,
			$"/rest/v1/task_completions?household_id=eq.{householdId}"
			+ "&select=task_id,occurrence_date,profile_id,xp_earned,completed_at,task:tasks(title),profile:profiles(display_name)"
			+ $"&order=completed_at.desc&limit={limit}");
	private Task<DbResult<List<TaskRecord>>> GetTasks(string filter) =>
		Send<List<TaskRecord>>(HttpMethod.Get, $"/rest/v1/tasks?{filter}&{TaskSelect}&order=start_date.asc,created_at.asc");
	private static string DateString(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
 
	private Task<DbResult<T>> Rpc<T>(string function, object args) =>
		Send<T>(HttpMethod.Post, "/rest/v1/rpc/" + function, args);
 
	private async Task<DbResult<T>> Send<T>(HttpMethod method, string path, object body = null,
		bool useAuth = true, string prefer = null)
	{
		// Refresh the login token shortly before it expires.
		if (useAuth && _refreshToken != "" && Now() > _expiresAt - 60)
			await RefreshAsync();
		using var req = new HttpRequestMessage(method, SupabaseUrl + path);
		req.Headers.Add("apikey", AnonKey);
		// Only send Authorization when logged in. The publishable keys must NOT go here.
		if (useAuth && _accessToken != "")
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
		if (prefer != null)
			req.Headers.Add("Prefer", prefer);
		if (body != null)
			req.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
 
		try
		{
			using var res = await Http.SendAsync(req);
			string text = await res.Content.ReadAsStringAsync();
 
			if (!res.IsSuccessStatusCode)
				return Fail<T>(ErrorText(text, (int)res.StatusCode));
 
			T data = string.IsNullOrWhiteSpace(text) ? default : JsonSerializer.Deserialize<T>(text, JsonOptions);
			return new DbResult<T> { Ok = true, Data = data };
		}
		catch (HttpRequestException)
		{
			return Fail<T>("No connection to the server (check your internet)");
		}
		catch (TaskCanceledException)
		{
			return Fail<T>("The server took too long to answer");
		}
		catch (JsonException e)
		{
			return Fail<T>("Unexpected answer from the server: " + e.Message);
		}
	}
	private async Task<bool> RefreshAsync()
	{
		string token = _refreshToken;
		_refreshToken = "";   // stops Send from trying to refresh again
		var res = await Send<AuthSession>(HttpMethod.Post, "/auth/v1/token?grant_type=refresh_token",
			new { refresh_token = token }, useAuth: false);
 
		if (res.Ok && !string.IsNullOrEmpty(res.Data?.AccessToken))
		{
			SetSession(res.Data);
			return true;
		}
		ClearSession();
		return false;
	}
	private void SetSession(AuthSession s)
	{
		bool sameUser = s.User != null && s.User.Id == UserId;
		_accessToken = s.AccessToken ?? "";
		_refreshToken = s.RefreshToken ?? "";
		_expiresAt = Now() + (s.ExpiresIn > 0 ? s.ExpiresIn : 3600);
		UserId = s.User?.Id ?? Guid.Empty;
		if (!sameUser) PlayingAsId = UserId;   // a token refresh keeps the child selection
 
		var cfg = new ConfigFile();
		cfg.SetValue("auth", "refresh_token", _refreshToken);
		cfg.Save(SessionFile);
		EmitSignal(SignalName.SessionChanged, true);
	}
 
	private void ClearSession()
	{
		_accessToken = "";
		_refreshToken = "";
		_expiresAt = 0;
		UserId = Guid.Empty;
		PlayingAsId = Guid.Empty;
		if (FileAccess.FileExists(SessionFile))
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SessionFile));
		EmitSignal(SignalName.SessionChanged, false);
	}
 
	private static double Now() => Time.GetUnixTimeFromSystem();
 
	private static DbResult<T> Fail<T>(string error) => new() { Ok = false, Error = error };
 
	private static DbResult<T> First<T>(DbResult<List<T>> res) => res.Ok
		? new DbResult<T> { Ok = true, Data = res.Data != null && res.Data.Count > 0 ? res.Data[0] : default }
		: Fail<T>(res.Error);
 
	private static string ErrorText(string text, int code)
	{
		try
		{
			using var doc = JsonDocument.Parse(text);
			if (doc.RootElement.ValueKind == JsonValueKind.Object)
				foreach (string key in new[] { "message", "msg", "error_description", "error" })
					if (doc.RootElement.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
						return v.GetString();
		}
		catch (JsonException) { }
		return $"Server error {code}: {text}";
	}
}