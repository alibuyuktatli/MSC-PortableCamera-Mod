using MSCLoader;
using System.Security.AccessControl;
using System.IO;
using UnityEngine;
using System;

namespace PortableCamera
{
    public class PortableCamera : Mod
    {
        public override string ID => "PortableCamera"; // Your (unique) mod ID 
        public override string Name => "Portable Camera"; // Your mod name
        public override string Author => "Fartcollector1"; // Name of the Author (your name)
        public override string Version => "1.0"; // Version
        public override string Description => "This mod allows you to grab a portable camera."; // Short description of your mod

        string screenshotFoldername = "camera_photos";
        string screenshotName = "NULL";

        GameObject hud;

        float dt;

        // KeyBinds
        SettingsKeybind keyb_switchcam, keyb_screenshot, keyb_bringcam, keyb_freezecam,
            keyb_rotleft,
            keyb_rotright,
            keyb_rotdown,
            keyb_rotup,

            keyb_moveleft,
            keyb_moveright,
            keyb_movedown,
            keyb_moveup,
            keyb_moveforward,
            keyb_moveback;
       // other
        SettingsSliderInt slider_camfov;
        SettingsSlider slider_cammovespeed, slider_camrotspeed;
        SettingsCheckBox check_freezecam, check_hidehud;

        SettingsButton button_bringcam;



        
        bool hideHUDwhileSwitched = true;

        bool switchedCam = false;

        GameObject camitem;

        Camera playerCam;
        Camera cameraitemCam;

        GameObject GetCamObject()
        {
            GameObject[] items = GameObject.FindGameObjectsWithTag("ITEM");
            foreach (GameObject g in items)
            {
                if (g.name == "camera(itemx)")
                {
                    return g;
                }
            }
            return null;
        }

        void CameraSetup()
        {
            camitem = GetCamObject();

            playerCam = Camera.main;

            cameraitemCam = GameObject.Instantiate(playerCam);
            cameraitemCam.name = "CameraItemCam";

            // PlayMaker
            foreach (var fsm in cameraitemCam.GetComponents<Component>())
                if (fsm.GetType().Name.Contains("PlayMaker"))
                    GameObject.Destroy(fsm);

            // MSCLoader raycast
            var ray = cameraitemCam.GetComponent("MSCLoader.UnifiedRaycast");
            if (ray != null)
                GameObject.Destroy(ray);

            GameObject.Destroy(cameraitemCam.GetComponent<AudioListener>());

            cameraitemCam.enabled = false;

            cameraitemCam.nearClipPlane = 0.1f;

            cameraitemCam.transform.SetParent(camitem.transform, false);
            cameraitemCam.transform.localPosition = Vector3.zero;
            cameraitemCam.transform.localRotation = Quaternion.Euler(0, 90, 90);


            hud = GameObject.Find("GUI");

            /*ModConsole.Log("------COMPS------");

            foreach (var c in playerCam.GetComponents<Component>())
            {
                if (c == null) continue;
                ModConsole.Log(c.GetType().FullName);
            }


            ModConsole.Log("------COMPS------");*/


        }

        void BringCamPlayer()
        {
            camitem.transform.position = playerCam.transform.position;
        }

        void Screenshot(string path, Camera targetCamera)
        {
            bool camstate = targetCamera.enabled;

            targetCamera.enabled = true;
            RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            targetCamera.targetTexture = renderTexture;
            targetCamera.Render();

            Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshot.Apply();

            byte[] bytes = screenshot.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);

            targetCamera.targetTexture = null;
            RenderTexture.active = null;
            GameObject.Destroy(renderTexture);
            GameObject.Destroy(screenshot);

            targetCamera.enabled = camstate;
        }


        public override void ModSetup()
        {
            SetupFunction(Setup.OnMenuLoad, Mod_OnMenuLoad);
            SetupFunction(Setup.OnNewGame, Mod_OnNewGame);
            SetupFunction(Setup.PreLoad, Mod_PreLoad);
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.PostLoad, Mod_PostLoad);
            SetupFunction(Setup.OnSave, Mod_OnSave);
            SetupFunction(Setup.OnGUI, Mod_OnGUI);
            SetupFunction(Setup.Update, Mod_Update);
            SetupFunction(Setup.FixedUpdate, Mod_FixedUpdate);
            SetupFunction(Setup.ModSettings, Mod_Settings);
        }

        private void Mod_Settings()
        {
            // All settings should be created here. 
            // DO NOT put anything that isn't settings or keybinds in here!

            Keybind.AddHeader("Camera");
            keyb_switchcam = Keybind.Add("keyb_switchcam", "Switch Camera", KeyCode.Backslash, KeyCode.Backspace);
            keyb_screenshot = Keybind.Add("keyb_screenshot", "Screenshot", KeyCode.LeftAlt, KeyCode.Slash);
            //keyb_bringcam = Keybind.Add("keyb_bringcam", "Teleport Camera to Player", KeyCode.LeftAlt, KeyCode.L);
            keyb_freezecam = Keybind.Add("keyb_freezecam", "Freeze Camera", KeyCode.LeftAlt, KeyCode.L);

            Keybind.AddHeader("ROTATION - Camera Controls");
            keyb_rotleft = Keybind.Add("keyb_rotleft", "Rotate Camera LEFT", KeyCode.LeftAlt, KeyCode.LeftArrow);
            keyb_rotright = Keybind.Add("keyb_rotright", "Rotate Camera RIGHT", KeyCode.LeftAlt, KeyCode.RightArrow);
            keyb_rotdown = Keybind.Add("keyb_rotdown", "Rotate Camera DOWN", KeyCode.LeftAlt, KeyCode.DownArrow);
            keyb_rotup = Keybind.Add("keyb_rotup", "Rotate Camera UP", KeyCode.LeftAlt, KeyCode.UpArrow);

            Keybind.AddHeader("MOVEMENT - Camera Controls");
            keyb_moveleft = Keybind.Add("keyb_moveleft", "Move Camera LEFT", KeyCode.LeftShift, KeyCode.LeftArrow);
            keyb_moveright = Keybind.Add("keyb_moveright", "Move Camera RIGHT", KeyCode.LeftShift, KeyCode.RightArrow);
            keyb_movedown = Keybind.Add("keyb_movedown", "Move Camera DOWN", KeyCode.LeftShift, KeyCode.PageDown);
            keyb_moveup = Keybind.Add("keyb_moveup", "Move Camera UP", KeyCode.LeftShift, KeyCode.PageUp);
            keyb_moveforward = Keybind.Add("keyb_moveforward", "Move Camera FORWARD", KeyCode.LeftShift, KeyCode.UpArrow);
            keyb_moveback = Keybind.Add("keyb_moveback", "Move Camera BACK", KeyCode.LeftShift, KeyCode.DownArrow);

            Settings.AddHeader("Camera Settings");
            slider_camfov = Settings.AddSlider("cam_fov", "Camera FOV", 10, 200, 90);
            slider_cammovespeed = Settings.AddSlider("cam_movespeed", "Camera Move Speed", 0.5f, 1000f, 50f);
            slider_camrotspeed = Settings.AddSlider("cam_rotspeed", "Camera Rotation Speed", 0.5f, 1000f, 50f);

            check_freezecam = Settings.AddCheckBox("cam_freeze", "Freeze Camera", false);
            check_hidehud = Settings.AddCheckBox("hide_hud", "Hide HUD when using camera", true);

            Settings.AddHeader("Other");
            button_bringcam = Settings.AddButton("Teleport camera to player", BringCamPlayer);
        }
       
        private void Mod_OnMenuLoad()
        {
            // Called once, when the mod is loaded in the main menu
        }
        private void Mod_OnNewGame()
        {
            // Called once, when creating a new game. This is useful for deleting old mod saves
            CameraSetup();
            //camitem.transform.position = new Vector3(-9.9f, 0.5f, 14.2f);
        }
        private void Mod_PreLoad()
        {
            // Called once, right after GAME scene loads but before the game is fully loaded
        }
        private void Mod_OnLoad()
        {
            // Called once, when mod is loading after game is fully loaded
            CameraSetup();
        }
        private void Mod_PostLoad()
        {
            // Called once, after all mods finished OnLoad
        }
        private void Mod_OnSave()
        {
            // Called once, when save and quit
            // Serialize your save file here.
        }
        private void Mod_OnGUI()
        {
            // Draw unity OnGUI() here
        }
        private void Mod_Update()
        {
            // Update is called once per frame
            if (camitem == null) return;
            if (cameraitemCam == null) return;
            if (hud == null) return;

            dt = Time.deltaTime;

            camitem.GetComponent<Rigidbody>().isKinematic = check_freezecam.GetValue();

            if (keyb_switchcam.GetKeybindDown())
            {
                switchedCam = !switchedCam;

                playerCam.enabled = !switchedCam;
                cameraitemCam.enabled = switchedCam;

                if (check_hidehud.GetValue()) //if true
                {
                    hud.SetActive(!switchedCam);
                }
                



                //ModConsole.Log(Vector3.Distance(playerCam.transform.position, cameraitemCam.transform.position));
                //ModConsole.Log(playerCam.transform.position);
            }

            if (keyb_freezecam.GetKeybindDown())
            {
                check_freezecam.SetValue(!check_freezecam.GetValue());
            }

            /*if (keyb_bringcam.GetKeybindDown())
            {
                camitem.transform.position = playerCam.transform.position + playerCam.transform.forward * 1.5f;
            }*/

            if (keyb_screenshot.GetKeybindDown())
            {
                string time = DateTime.Now.ToShortTimeString();
                string date = DateTime.Now.ToShortDateString();
                Directory.CreateDirectory(screenshotFoldername);
                Screenshot($"{screenshotFoldername}/{date}-{time}-{Environment.UserName}", cameraitemCam);
            }
            
            cameraitemCam.fieldOfView = slider_camfov.GetValue();

            Vector3 camitempos = camitem.transform.position;
            Quaternion camitemrot = camitem.transform.rotation;

            // CAMERA MOVING
            if (keyb_moveleft.GetKeybind()) camitem.transform.position -= cameraitemCam.transform.right * slider_cammovespeed.GetValue() * dt;
            if (keyb_moveright.GetKeybind()) camitem.transform.position += cameraitemCam.transform.right * slider_cammovespeed.GetValue() * dt;
            if (keyb_moveforward.GetKeybind()) camitem.transform.position += cameraitemCam.transform.forward * slider_cammovespeed.GetValue() * dt;
            if (keyb_moveback.GetKeybind()) camitem.transform.position -= cameraitemCam.transform.forward * slider_cammovespeed.GetValue() * dt;
            if (keyb_moveup.GetKeybind()) camitem.transform.position += cameraitemCam.transform.up * slider_cammovespeed.GetValue() * dt;
            if (keyb_movedown.GetKeybind()) camitem.transform.position -= cameraitemCam.transform.up * slider_cammovespeed.GetValue() * dt;



            if (keyb_rotleft.GetKeybind())
                camitem.transform.Rotate(0, -slider_camrotspeed.GetValue() * dt, 0, Space.World);
            if (keyb_rotright.GetKeybind())
                camitem.transform.Rotate(0, slider_camrotspeed.GetValue() * dt, 0, Space.World);
            if (keyb_rotup.GetKeybind())
                camitem.transform.rotation *= Quaternion.Euler(Vector3.down * slider_camrotspeed.GetValue() * dt);
            if (keyb_rotdown.GetKeybind())
                camitem.transform.rotation *= Quaternion.Euler(Vector3.up * slider_camrotspeed.GetValue() * dt);



        }

        private void Mod_FixedUpdate()
        {
            //camitem = GetCamObject();
            /*if (Input.GetKeyDown(KeyCode.K))
            {
         
                GameObject go = hit.transform.gameObject;
                MSCLoader.ModConsole.Log("--"+go.name+"--");

                MSCLoader.ModConsole.Log("TAG: "+go.tag);
                MSCLoader.ModConsole.Log("LAYER: "+go.layer);
                MSCLoader.ModConsole.Log("POS: "+go.transform.position);
                MSCLoader.ModConsole.Log("ROT: "+go.transform.rotation);
                MSCLoader.ModConsole.Log("ACTIVE: "+go.activeSelf);
                MSCLoader.ModConsole.Log("MATERIAL: "+go.GetComponent<Renderer>()?.material.name);
                    
                MSCLoader.ModConsole.Log("--" + go.name + "--\n");

                MSCLoader.ModConsole.Log("TAG: " + camitem.tag);
                MSCLoader.ModConsole.Log("LAYER: " + camitem.layer);
                MSCLoader.ModConsole.Log("POS: " + camitem.transform.position);
                MSCLoader.ModConsole.Log("ROT: " + camitem.transform.rotation);
                MSCLoader.ModConsole.Log("ACTIVE: " + camitem.activeSelf);
                MSCLoader.ModConsole.Log("MATERIAL: " + camitem.GetComponent<Renderer>()?.material.name);

            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                Vector3 pos = camitem.transform.position;
                camitem.transform.position = new Vector3(pos.x+5, pos.y, pos.z);
            }*/
        }
    }
}
