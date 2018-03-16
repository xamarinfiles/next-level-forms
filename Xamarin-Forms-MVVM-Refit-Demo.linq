<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.ComponentModel.DataAnnotations.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>Castle.Core</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference Version="3.1.0">Refit</NuGetReference>
  <NuGetReference>Xamarin.Forms</NuGetReference>
  <Namespace>Castle.DynamicProxy</Namespace>
  <Namespace>Refit</Namespace>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Collections.ObjectModel</Namespace>
  <Namespace>System.ComponentModel.DataAnnotations</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Xamarin.Forms</Namespace>
  <CopyLocal>true</CopyLocal>
</Query>

// Demo for talks at Orlando Code Camp 2018 showing Refit, MVVM, and Xamarin.Forms Commands inside LinqPad

#region Imports

// Namespace Imports - .Net Framework:
// System
// System.Collections.Generic
// System.Collections.ObjectModel
// System.ComponentModel.DataAnnotations
// System.Net.Http
// System.Threading.Tasks

// Namespace Imports - NuGet:
// Castle.DynamicProxy
// Refit 3.X
// Xamarin.Forms

#endregion

#region View in MVVM

void Main()
{
	// Global values (App.xaml.cs or App.cs)
	CodeCampDataService dataService = new CodeCampDataService();
	// BREAKPOINT
	NextLevelFormsViewModel viewModel = new NextLevelFormsViewModel(dataService);

	// NO BREAKPOINT
	// View: ActivityIndicator on Page in Xamarin.Forms
	while (viewModel.IsWaiting)	{}

	// BREAKPOINT
	viewModel.FilterSessionsCommand.Execute("Xamarin");

	// BREAKPOINT
	viewModel.FilterSessionsCommand.Execute("Forms");
}

#endregion

#region Models in MVVM (data schema)

public class Session
{
	// API
	public int Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string KeyWords { get; set; }
	public bool? Special { get; set; }
	public int SpeakerId { get; set; }
	public int? TimeslotId { get; set; }
	public int? TrackId { get; set; }

	// Populated from joins
	public string SpeakerName { get; set; }
	public string TimeslotName { get; set; }
	public string TrackName { get; set; }
}

public class Speaker
{
	// API
	public int Id { get; set; }
	public string FullName { get; set; }
	public bool? Special { get; set; }
}

public class Timeslot
{
	// API
	public int Id { get; set; }
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public int Rank { get; set; }
	public bool Special { get; set; }
}

public class Track
{
	// API
	public int Id { get; set; }
	public string Name { get; set; }
}

#endregion

#region ViewModel in MVVM (state)

public class NextLevelFormsViewModel
{
	private readonly CodeCampDataService DataService;

	private IList<Session> _contentSessions { get; set;} = new List<Session>();

	public NextLevelFormsViewModel(CodeCampDataService dataService)
	{
		DataService = dataService;

		// BREAKPOINT
		GetSessionsCommand.Execute(null);
	}

	public ObservableCollection<Session> FilteredSessions { get; set; } 
		= new ObservableCollection<Session>();

	public bool IsWaiting { get; set; }

	public Command<string> FilterSessionsCommand =>
		new Command<string>(filterText =>
		{
			// BREAKPOINT
			IsWaiting = true;
			FilterSessionsCommand.ChangeCanExecute();

			// Filter IList<Session> from API by filter text (in memory => fast for small # of Sessions)
			var _filterTextLower = filterText.ToLower();
			// HACK Doesn't do word or regex match => can lead to false-positive results
			var _filteredSessions = _contentSessions
				.Where(s => 
					s.Name.ToLower().Contains(_filterTextLower)
					|| s.Description.ToLower().Contains(_filterTextLower)
					|| (s.KeyWords != null && s.KeyWords.ToLower().Contains(_filterTextLower))
				)
				.ToList();

			// Load into ObservableCollection<Session> for View (in memory => fast for small # of Sessions)
			FilteredSessions.Clear();
			foreach (var sessions in _filteredSessions)
			{
				FilteredSessions.Add(sessions);
			}

			FilteredSessions.Count().Dump($"# of {filterText} sessions");
			FilteredSessions.Dump(exclude: "Special,SpeakerId,TimeslotId,TrackId");

			// BREAKPOINT
			IsWaiting = false;
			FilterSessionsCommand.ChangeCanExecute();
		},
		filterText => !IsWaiting);

	public Command GetSessionsCommand =>
		new Command(async () =>
		{
			// BREAKPOINT
			IsWaiting = true;
			GetSessionsCommand.ChangeCanExecute();

			// BREAKPOINT
			// Get sessions from API (service call => most time-consuming operation)
			_contentSessions = await DataService.GetSessions();

			// BREAKPOINT
			// Load into ObservableCollection<Session> for View (in memory => fast for small # of Sessions)
			FilteredSessions.Clear();
			foreach (var sessions in _contentSessions)
			{
				FilteredSessions.Add(sessions);
			}

			// BREAKPOINT
			IsWaiting = false;
			GetSessionsCommand.ChangeCanExecute();
		},
		() => !IsWaiting);
}

#endregion

#region Data Service (data access)

public class CodeCampDataService
{
	private const string ApiUrl = "http://www.orlandocodecamp.com/api/";

	public async Task<IList<Session>> GetSessions()
	{
		IList<Session> contentSessions = new List<Session>();

		// Speakers
		IList<Speaker> speakerList = await CodeCampApi.GetSpeakers();
		IDictionary<int, string> speakerDictionary = speakerList
			.Where(t => t.Special != true)
			// NOTE Speaker.FullName is empty in some cases (web site and API need to be updated)
			.ToDictionary(t => t.Id, t => t.FullName);
		speakerDictionary.Count().Dump("# of speakers");
		//speakerDictionary.Dump();

		// Timeslots
		IList<Timeslot> timeslotList = await CodeCampApi.GetTimeslots();
		IDictionary<int, string> timeslotDictionary = timeslotList
			.Where(t => t.Special != true)
			.ToDictionary(t => t.Id, t => $"{t.StartTime:hh:mm tt} to {t.EndTime:hh:mm tt}");
		timeslotDictionary.Count().Dump("# of timeslots");
		//timeslotDictionary.Dump();

		// Tracks
		IList<Track> trackList = await CodeCampApi.GetTracks();
		IDictionary<int, string> trackDictionary = trackList
			.ToDictionary(t => t.Id, t => t.Name);
		trackDictionary.Count().Dump("# of tracks");
		//trackDictionary.Dump();

		// Sessions
		IList<Session> allSessions = await CodeCampApi.GetSessions();
		allSessions.Count().Dump("# of sessions");
		//allSessions.Dump();

		contentSessions = allSessions
			.Where(cs => cs.Special != true)
			.OrderBy(cs => cs.TimeslotId)
			.ThenBy(cs => cs.TrackId)
			.ToList();
		foreach (var session in contentSessions)
		{
			session.SpeakerName = speakerDictionary[session.SpeakerId];
			if (session.TrackId != null)
				session.TrackName = trackDictionary[(int) session.TrackId];
			if (session.TimeslotId != null)
				session.TimeslotName = timeslotDictionary[(int) session.TimeslotId];
		}
		contentSessions.Count().Dump("# of content sessions");
		//contentSessions.Dump();

		return contentSessions;
	}

#region Refit

	public interface ICodeCampApi
	{
		[Get("/SessionList")]
		Task<IList<Session>> GetSessions();

		[Get("/SpeakerList")]
		Task<IList<Speaker>> GetSpeakers();

		[Get("/TimeslotList")]
		Task<IList<Timeslot>> GetTimeslots();

		[Get("/TrackList")]
		Task<IList<Track>> GetTracks();
	}

	public ICodeCampApi CodeCampApi { get; set; }
		= ProxyRestService.For<ICodeCampApi>(ApiUrl);
		//// The real call to Refit in an app is this
		// = RestService.For<ICodeCampApi>(ApiUrl);
}

#endregion

#endregion

#region Utlity Code

// HACK to use Refit inside LinqPad (requires Refit 2.X to 3.X):
// From https://gist.github.com/bennor/c73870e810f8245b2b1d

public class ProxyRestService
{
	static readonly ProxyGenerator Generator = new ProxyGenerator();

	public static T For<T>(HttpClient client)
		where T : class
	{
		if (!typeof(T).IsInterface)
		{
			throw new InvalidOperationException("T must be an interface.");
		}

		var interceptor = new RestMethodInterceptor<T>(client);
		return Generator.CreateInterfaceProxyWithoutTarget<T>(interceptor);
	}

	public static T For<T>(string hostUrl)
		where T : class
	{
		var client = new HttpClient() { BaseAddress = new Uri(hostUrl) };
		return For<T>(client);
	}

	class RestMethodInterceptor<T> : IInterceptor
	{
		static readonly Dictionary<string, Func<HttpClient, object[], object>> methodImpls
			= RequestBuilder.ForType<T>().InterfaceHttpMethods
				.ToDictionary(k => k, v => RequestBuilder.ForType<T>().BuildRestResultFuncForMethod(v));

		readonly HttpClient client;

		public RestMethodInterceptor(HttpClient client)
		{
			this.client = client;
		}

		public void Intercept(IInvocation invocation)
		{
			if (!methodImpls.ContainsKey(invocation.Method.Name))
			{
				throw new NotImplementedException();
			}
			invocation.ReturnValue = methodImpls[invocation.Method.Name](client, invocation.Arguments);
		}
	}
}

#endregion