using System;
using SubWhitelist;
using System.IO;
using SDTM;
using System.Net;
using System.Collections;
using SDTM.Data;

namespace SubWhitelist_Twitch
{
	public class TwitchAuthProvider:AuthProviderAbstract
	{
		public TwitchAuthProvider (string basePath)
		{
			this.Name = "twitch";
			this.AllowDescription = "Allow users who are twitch Followers";

			SDTM.Servers.HTTP.WWW._templates.Add ("subwhitelisttwitchsignup", File.ReadAllText (basePath + Path.DirectorySeparatorChar + "html" + Path.DirectorySeparatorChar + "tpl_twitchsignup.html"));
			SDTM.Servers.HTTP.WWW._templates.Add ("subwhitelisttwitchsignupreturn", File.ReadAllText (basePath + Path.DirectorySeparatorChar + "html" + Path.DirectorySeparatorChar + "tpl_twitchsignupreturn.html"));
			SDTM.Servers.HTTP.WWW._templates.Add ("subwhitelisttwitchsettings", File.ReadAllText (basePath + Path.DirectorySeparatorChar + "html" + Path.DirectorySeparatorChar + "tpl_twitchsettings.html"));
		}

		public override string getSignupHTML(WWWRequest request){
			string html = SDTM.Servers.HTTP.WWW._templates ["subwhitelisttwitchsignup"];

			html = html.Replace ("{twitch_redirect_uri}", "{host_and_port}/subwhitelist/signup/twitch");
			html = html.Replace ("{twitch_name}", subwhitelist.Settings.Get("twitch_name"));
			html = html.Replace ("{twitch_client_id}", subwhitelist.Settings.Get("twitch_client_id"));

			return html;
		}

		public override string getSettingHTML(WWWRequest request){
			if (request.Form.Count > 0) {

				if (request.Form.ContainsKey ("allow_twitch")) {
					subwhitelist.Settings.Set ("allow_twitch", "true");
					subwhitelist.Settings.Set ("twitch_name", request.Form ["twitch_name"]);
                    subwhitelist.Settings.Set ("twitch_client_id", request.Form ["twitch_client_id"]);
					subwhitelist.Settings.Set ("twitch_client_secret", request.Form ["twitch_client_secret"]);
                    subwhitelist.Settings.Set( "twitch_channel_id", getChannelId(request.Form["twitch_client_id"], request.Form["twitch_name"]));
                    subwhitelist.Settings.Set("twitch_min_sub", request.Form["twitch_min_sub"]);
                } else {
					subwhitelist.Settings.Set ("allow_twitch", "false");
					subwhitelist.Settings.Set ("twitch_name", "");
					subwhitelist.Settings.Set ("twitch_client_id", "");
					subwhitelist.Settings.Set ("twitch_client_secret", "");
                    subwhitelist.Settings.Set("twitch_channel_id","");
                    subwhitelist.Settings.Set("twitch_min_sub", "");
                }

				subwhitelist.Settings.Save ();
			}

			string html = SDTM.Servers.HTTP.WWW._templates ["subwhitelisttwitchsettings"];
			html = html.Replace("{twitch_js_origin}", "{host_and_port}");
			html = html.Replace ("{twitch_redirect_uri}", "{host_and_port}/subwhitelist/signup/twitch");
			html = html.Replace ("{twitch_name}", subwhitelist.Settings.Get("twitch_name"));
			html = html.Replace ("{twitch_client_id}", subwhitelist.Settings.Get("twitch_client_id"));
			html = html.Replace ("{twitch_client_secret}", subwhitelist.Settings.Get("twitch_client_secret"));
            html = html.Replace("{twitch_min_sub}", subwhitelist.Settings.Get("twitch_min_sub"));

            //html = html.Replace ("{allow_token}", subwhitelist.Settings.Get("allow_token"));
            html = html.Replace ("{allow_twitch}", subwhitelist.Settings.Get("allow_twitch"));

			return html;
		}



		public override WWWEndpointProvider getSignupEndpoint(){
			return new EndPoint_TwitchSignup ();
		}

		public override WWWEndpointProvider getSettingEndpoint(){
			return null;//new WWWEndpointProvider ();
		}

		public override string Validate(string accessToken){
			//string streamerName = "";
			if (subwhitelist.Settings.Get("twitch_name")!="") {
				//streamerName = subwhitelist.Settings.Get("twitch_name");
				//ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;

				string twitchClientId = "";
				string twitchClientSecret = "";
                string minSubString = "";
                int minSub = -1;
				if (subwhitelist.Settings.Get ("twitch_client_id")!=null) {
					twitchClientId = subwhitelist.Settings.Get("twitch_client_id");
				}

				if (subwhitelist.Settings.Get ("twitch_client_secret")!=null) {
					twitchClientSecret = subwhitelist.Settings.Get("twitch_client_secret");
				}

                if (subwhitelist.Settings.Get("twitch_min_sub") != null) {
                    minSubString = subwhitelist.Settings.Get("twitch_min_sub");
                    int.TryParse(minSubString, out minSub);
                }
				string url = "https://api.twitch.tv/kraken/user";
				//string urlParams = "code=" + authCode + "&grant_type=authorization_code&client_id=" + twitchClientId + "&client_secret=" + twitchClientSecret + "&redirect_uri=" + redirect;

				try{
					using (WebClient client = new WebClient ()) {
						client.Headers.Add("Accept", "application/vnd.twitchtv.v5+json");
						client.Headers.Add("Authorization", "OAuth "+accessToken);
						client.Headers.Add("Client-ID", twitchClientId);
						string response = client.DownloadString(url);
                        //
                        //string res = System.Text.Encoding.UTF8.GetString(response);
                        Hashtable jsonObj = (Hashtable)JSON.JsonDecode(response);
                        string userId = jsonObj["_id"].ToString();
                        string channelId = subwhitelist.Settings.Get("twitch_channel_id");
                        string subUrl = "https://api.twitch.tv/kraken/users/"+userId+"/subscriptions/"+channelId+"?api_version=5";
                        
                        string subResponse = client.DownloadString(subUrl);
                        Hashtable subJson = (Hashtable)JSON.JsonDecode(subResponse);
                        Log.Out("Subbed at level: "+subJson["sub_plan"].ToString());
                        string subPlanString = subJson["sub_plan"].ToString();
                        int subPlan = -1;
                        int.TryParse(subPlanString, out subPlan);
                        if (subPlan > -1 && minSub > -1) {
                            if (subPlan >= minSub) {
                               return userId;
                            }
                            else
                            {
                               return "error: You cannot access this server at your current Subscriber level.";
                            }
                        }
                        else
                        {
                            return "error: Invalid Twitch Sub Settings.";
                        }
                    }
				}catch(WebException e){
                    var resp = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                    if(resp.Contains("Not Found"))
                    {

                        string channelName = subwhitelist.Settings.Get("twitch_name");
                        return "error: You are not subscribed to " + channelName;
                    }

                    return "error: Request Error";
                }

				return "error: invalid sub";
			} else {
				return "error:no twitch name";
			}		
		}

        public string getChannelId(string clientId, string twitchName) {
            string channelId = "";
            string url = "https://api.twitch.tv/kraken/users";
            string urlParams = "?login=" + twitchName;

			try{
				using (WebClient client = new WebClient ()) {
					client.Headers.Add("Accept", "application/vnd.twitchtv.v5+json");
					//client.Headers.Add("Authorization", "OAuth "+accessToken);
					client.Headers.Add("Client-ID", clientId);
					string response = client.DownloadString(url+urlParams);
                    Log.Out(response);
                    //string res = System.Text.Encoding.UTF8.GetString(response);
                    Hashtable jsonObj = (Hashtable)JSON.JsonDecode(response);
                    double userCount = (double)jsonObj["_total"];
                    Log.Out(userCount.ToString());
                    if (userCount == 1)
                    {
                        ArrayList userList = (ArrayList)jsonObj["users"];
                        object o = userList[0];
                        Hashtable user = (Hashtable)o;
                        Log.Out(user["_id"].ToString());
                        channelId = user["_id"].ToString();
                    }
                }
			}catch(Exception e){
				Console.WriteLine (e.Message);
			}

			return channelId;
        }
	}
}

