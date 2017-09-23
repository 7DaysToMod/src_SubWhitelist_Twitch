using System;
using SubWhitelist;

namespace SubWhitelist_Twitch
{
	public class SubWhitelist_Twitch:ModApiAbstract
	{
		public SubWhitelist_Twitch ()
		{
			SDTM.API.Events.OnGameStartDone += OnGameStartDone;
		}

		public void OnGameStartDone(){
			string basePath = "";

			if (basePath == "") {
				foreach (Mod m in ModManager.GetLoadedMods()) {
					if (m.ModInfo.Name == "SubWhitelist_Twitch") {
						basePath = m.Path;
					}
				}
			}

			subwhitelist.authProviders.Add ("twitch", new TwitchAuthProvider (basePath));
		}
	}
}

