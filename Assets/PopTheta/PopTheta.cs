using UnityEngine;
using System.Collections;
using System.Collections.Generic;




//	json commands & replies
namespace PopThetaCommand
{
	public class Params
	{
	};

	//	
	[System.Serializable]
	public class Execute
	{
		public string		name;
		public Params		parameters;
	};

	public class StartSession : Execute
	{
		public string	Url = "/osc/commands/execute";
		public static string	UrlStatic = "/osc/commands/execute";

		public StartSession() : base()
		{
			name = "camera.startSession";
		}
	}

	public class StartSessionReply
	{
		[System.Serializable]
		public class Results
		{
			public string	sessionId;
			public int		timeout;
		};
		
		public string	name = "camera.startSession";
		public string	state = "done";
		public Results	results;
	}
}



public class PopThetaSession
{
	public string	sessionId;

	public PopThetaSession(string _sessionId)
	{
		this.sessionId = _sessionId;
	}
}



/*
 API reference
 https://developers.theta360.com/en/docs/v2.1/api_reference/
 https://developers.google.com/streetview/open-spherical-camera/

*/
public class PopTheta : MonoBehaviour {

	//	fixed
	private string		Address = "192.168.1.1:80";
	public bool			DebugJsonResponse = true;

	private bool		StartedConnect = false;

	public UnityEngine.Events.UnityEvent	OnSessionStarted;
	public UnityEngine.Events.UnityEvent	OnSessionEnded;


	PopThetaSession		Session;

	void Update () {
	
		if (!StartedConnect) {
			StartedConnect = true;
			Connect ();
		}
	}

	void Connect()
	{
		var Command = new PopThetaCommand.StartSession ();
		System.Action<PopThetaCommand.StartSessionReply> OnReply = (PopThetaCommand.StartSessionReply Reply) => {
			Debug.Log ("Reply from start session: sessionid = " + Reply.results.sessionId);
			Session = new PopThetaSession( Reply.results.sessionId );
			OnSessionStarted.Invoke();
		};
		System.Action<string> OnError = (Error) => {
			Debug.Log ("Error from start session: " + Error);
		};

		SendCommand(Command.Url,Command, OnReply, OnError);

	}

	//	gr: should be able to get CommandUrl from the type...
	void SendCommand<SENDCOMMAND,RECVCOMMAND>(string CommandUrl,SENDCOMMAND Command,System.Action<RECVCOMMAND> OnResponse,System.Action<string> OnError)
	{
		//var x = Command.Url;
		var url = "http://" + Address + CommandUrl;
		var Content = JsonUtility.ToJson (Command);
		Debug.Log ("Sending " + typeof(SENDCOMMAND).Name + " to " + url);
		StartCoroutine( SendCommandImpl(Command, url, Content, OnResponse, OnError ) );
	}

	IEnumerator SendCommandImpl<SENDCOMMAND,RECVCOMMAND>(SENDCOMMAND Command,string Url,string Content,System.Action<RECVCOMMAND> OnResponse,System.Action<string> OnError)
	{
		var ContentBytes = System.Text.Encoding.UTF8.GetBytes( Content );
		var Headers = new Dictionary<string,string>();
		WWW http = new WWW (Url, ContentBytes, Headers);

		yield return http;

		if (http.error!=null) {
			OnError.Invoke (http.error);
			yield break;
		}

		//	convert response to json
		try
		{
			var Json = System.Text.Encoding.UTF8.GetString( http.bytes );
			if ( DebugJsonResponse )
				Debug.Log("reply: " + Json );
			var Reply = JsonUtility.FromJson<RECVCOMMAND>( Json );
			OnResponse.Invoke( Reply );
		}
		catch(System.Exception e) {
			OnError.Invoke (e.Message);
		}

		yield break;
	}
}


