using MelonLoader;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading;
using System.Globalization;
using StressLevelZero.Props.Weapons;
using ModThatIsNotMod;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;

namespace BoneWorksProtube
{
    public class BoneWorksProtube : MelonMod
    {
        public static string configPath = Directory.GetCurrentDirectory() + "\\Mods\\dualwield\\";
        public static bool dualWield = false;
        private MelonPreferences_Category config;
        public static bool leftHanded = false;
        private GameObject player;
        public string[] sidearms = { "tool_stapler", "handgun_1911", "handgun_Eder22", "handgun_P350" };

        public override void OnApplicationStart()
        {
            config = MelonPreferences.CreateCategory("provolver");
            config.CreateEntry<bool>("leftHanded", false);
            config.SetFilePath("Mods/Provolver/Provolver_config.cfg");
            leftHanded = bool.Parse(config.GetEntry("leftHanded").GetValueAsString());
            InitializeProTube();
            Hooking.OnPostFireGun += new Action<Gun>(this.GunHooks_OnGunFire);
        }

        public static void saveChannel(string channelName, string proTubeName)
        {
            string fileName = configPath + channelName + ".pro";
            File.WriteAllText(fileName, proTubeName, Encoding.UTF8);
        }

        public static string readChannel(string channelName)
        {
            string fileName = configPath + channelName + ".pro";
            if (!File.Exists(fileName)) return "";
            return File.ReadAllText(fileName, Encoding.UTF8);
        }

        public static void dualWieldSort()
        {
            ForceTubeVRInterface.FTChannelFile myChannels = JsonConvert.DeserializeObject<ForceTubeVRInterface.FTChannelFile>(ForceTubeVRInterface.ListChannels());
            var pistol1 = myChannels.channels.pistol1;
            var pistol2 = myChannels.channels.pistol2;
            if ((pistol1.Count > 0) && (pistol2.Count > 0))
            {
                dualWield = true;
                MelonLogger.Msg("Two ProTube devices detected, player is dual wielding.");
                if ((readChannel("rightHand") == "") || (readChannel("leftHand") == ""))
                {
                    MelonLogger.Msg("No configuration files found, saving current right and left hand pistols.");
                    saveChannel("rightHand", pistol1[0].name);
                    saveChannel("leftHand", pistol2[0].name);
                }
                else
                {
                    string rightHand = readChannel("rightHand");
                    string leftHand = readChannel("leftHand");
                    MelonLogger.Msg("Found and loaded configuration. Right hand: " + rightHand + ", Left hand: " + leftHand);
                    // Channels 4 and 5 are ForceTubeVRChannel.pistol1 and pistol2
                    ForceTubeVRInterface.ClearChannel(4);
                    ForceTubeVRInterface.ClearChannel(5);
                    ForceTubeVRInterface.AddToChannel(4, rightHand);
                    ForceTubeVRInterface.AddToChannel(5, leftHand);
                }
            }
            else
            {
                MelonLogger.Msg("SINGLE WIELD");
            }
        }
        private async void InitializeProTube()
        {
            MelonLogger.Msg("Initializing ProTube gear...");
            await ForceTubeVRInterface.InitAsync(true);
            Thread.Sleep(10000);
            dualWieldSort();
        }

        public GameObject FindPlayer()
        {
            if ((Object)player != (Object)null)
                return player;
            GameObject[] gameObjectArray = GameObject.FindGameObjectsWithTag("Player");
            for (int index = 0; index < gameObjectArray.Length; ++index)
            {
                if (((Object)gameObjectArray[index]).name == "PlayerTrigger")
                {
                    player = gameObjectArray[index];
                    return gameObjectArray[index];
                }
            }
            return (GameObject)null;
        }

        private void GunHooks_OnGunFire(Gun gun)
        {
            try
            {
                if ((Object)gun == (Object)null)
                    return;
                int instanceId = ((Object)gun).GetInstanceID();
                GameObject player = FindPlayer();
                Gun gunInHand1 = Player.GetGunInHand(Player.rightHand);
                if ((Object)gunInHand1 != (Object)null && instanceId == ((Object)gunInHand1).GetInstanceID())
                {
                    // RIGHT
                    string name = ((Object)gun).name;
                    string logStr = "Player fired right hand gun. Name:" + name;
                    //MelonLogger.Msg(logStr);
                    handleGunType(name, (leftHanded && !dualWield)
                    ? ForceTubeVRChannel.pistol2 : ForceTubeVRChannel.pistol1);
                }
                //LEFT
                Gun gunInHand2 = Player.GetGunInHand(Player.leftHand);
                if ((Object)gunInHand2 == (Object)null || instanceId != ((Object)gunInHand2).GetInstanceID())
                    return;
                
                string name1 = ((Object)gun).name;
                string logStr1 = "Player fired left hand gun. Name:" + name1;
                //MelonLogger.Msg(logStr1);
                handleGunType(name1, (leftHanded && !dualWield)
                ? ForceTubeVRChannel.pistol1 : ForceTubeVRChannel.pistol2);
            }
            catch(Exception e) {
                MelonLogger.Msg("Error gun shoot " + e);
            }
        }
        public void handleGunType(string name, ForceTubeVRChannel channel)
        {
            string[] types = name.Split(' ');

            // TRIGGER LEFT HAPTIC
            if (!sidearms.Contains(types[0]))
            {
                ForceTubeVRInterface.Shoot(210, 125, 20f, channel);
            }
            else
            {
                ForceTubeVRInterface.Kick(210, channel);
            }
        }
    }
}
