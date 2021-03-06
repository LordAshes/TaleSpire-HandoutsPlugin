using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using BepInEx;
using Bounce.Unmanaged;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace LordAshes
{
    [BepInPlugin(Guid, "Handouts Plug-In", Version)]
    public class HandoutsPlugin : BaseUnityPlugin
    {
        // Plugin info
        private const string Guid = "org.lordashes.plugins.handouts";
        private const string Version = "1.0.5.0";

        // Configuration
        private ConfigEntry<KeyboardShortcut> triggerKey { get; set; }

        // Content directory
        private string dir = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("/")) + "/TaleSpire_CustomData/";

        // Handout form
        System.Windows.Forms.Form handout = null;
        System.Windows.Forms.PictureBox handoutPB = null;

        // Speech font name
        private string fontName = "NAL Hand SDF";

        // Active requests
        private List<string> last = new List<string>();

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            UnityEngine.Debug.Log("Lord Ashes Handouts Plugin Active.");

            if(!System.IO.Directory.Exists(dir))
            {
                UnityEngine.Debug.LogWarning("Lord Ashes Handouts Plugin requires the custom folder '" + dir + "'. This folder is missing on your device. Attempting to create.");
                try { System.IO.Directory.CreateDirectory(dir); } catch(Exception) { UnityEngine.Debug.LogError("Unable to make custom folder '" + dir + "'. Please create this folder manually."); }
            }
            if (!System.IO.Directory.Exists(dir+"Images/"))
            {
                UnityEngine.Debug.LogWarning("Lord Ashes Handouts Plugin requires the custom folder '" + dir + "Images/'. This folder is missing on your device. Attempting to create.");
                try { System.IO.Directory.CreateDirectory(dir+"Images/"); } catch (Exception) { UnityEngine.Debug.LogError("Unable to make custom folder '" + dir + "Images/'. Please create this folder manually."); }
            }

            triggerKey = Config.Bind("Hotkeys", "Open Handout Dialog Shortcut", new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl));

            try
            {
                System.IO.File.WriteAllBytes(dir + "Images/TaleSpire.ico", Convert.FromBase64String(taleSpireIcoBase64));
            }
            catch(Exception)
            {
                UnityEngine.Debug.LogError("Unable to make TaleSpire.ico file. The custom folder '"+dir+"/Images' may not exist or the plugin does not have permission to write to it.");
            }

            // Post plugin to the TaleSpire main page
            StateDetection.Initialize(this.GetType());
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            if(isBoardLoaded())
            {
                CheckChatRequests();

                if (triggerKey.Value.IsUp())
                {
                    SystemMessage.AskForTextInput("Handout","Specify Handout Name","OK",onSubmit,null,"Cancel",null,"");
                }
            }
        }

        /// <summary>
        /// Event handler for the submit input dialog. Sends handout request to all client (including self)
        /// </summary>
        /// <param name="handout">URL of handout or local file</param>
        private void onSubmit(string handout)
        {
            ChatManager.SendChatMessage("[Handout] " + handout, CreaturePresenter.AllCreatureAssets[0].Creature.CreatureId.Value);
        }

        /// <summary>
        /// Chech for handout requests in the chat
        /// </summary>
        public void CheckChatRequests()
        {
            List<string> current = new List<string>();

            TextMeshProUGUI[] texts = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
            for (int i = 0; i < texts.Length; i++)
            {
                if ((texts[i].name == "Text") && (texts[i].font.name == fontName) && (texts[i].text.Trim().StartsWith("[Handout] ")))
                {
                    current.Add(texts[i].text);
                    if (!last.Contains(texts[i].text))
                    {
                        // Create a new form with picturebox and the indicated image content
                        string request = texts[i].text.Substring("[Handout] ".Length);

                        // If file does not start with HTTP then it is a local file. Find the file matching the specifiction
                        bool validate = true;
                        if (!request.ToUpper().StartsWith("HTTP"))
                        {
                            string[] matches = System.IO.Directory.EnumerateFiles(dir+"Images/", request + ".*").ToArray();
                            if (matches.Count() <= 0)
                            {
                                SystemMessage.DisplayInfoText("Device does not have local handout '" + request + "'\r\nin 'TaleSpire\\TaleSpire_CustomData\\Images'");
                                validate = false;
                            }
                            else
                            {
                                request = matches[0];
                            }
                        }

                        if (validate)
                        {
                            handout = new System.Windows.Forms.Form();
                            handout.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
                            handout.BackColor = System.Drawing.Color.Black;
                            handout.ForeColor = System.Drawing.Color.Orange;
                            try { handout.Icon = new System.Drawing.Icon(dir + "TaleSpire.ico"); } catch (Exception) {; }
                            handout.Left = 0;
                            handout.Top = 0;
                            handout.Width = 10;
                            handout.Height = 10;
                            handoutPB = new System.Windows.Forms.PictureBox();
                            handoutPB.Left = 0;
                            handoutPB.Top = 0;
                            handoutPB.SizeChanged += (s, e) =>
                            {
                                handout.Width = handoutPB.Width + 20;
                                handout.Height = handoutPB.Height + 43;
                                handout.Left = (Screen.width - handout.Width) / 2;
                                handout.Top = (Screen.height - handout.Height) / 2;
                            };
                            handoutPB.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
                            handout.Controls.Add(handoutPB);
                            handout.Text = "Handout";
                            handout.ControlBox = true;
                            handout.MinimizeBox = false;
                            handout.MaximizeBox = false;

                            // Load the specified file
                            try
                            {
                                UnityEngine.Debug.Log("Loading Handout '" + request + "'");
                                handoutPB.Load(request);
                                handout.Show();
                            }
                            catch (Exception)
                            {
                                SystemMessage.DisplayInfoText("Trouble accessing handout\r\n'" + request + "'");
                                handout.Hide();
                                handout.Dispose();
                            }
                        }
                    }
                }
            }
            last = current;
        }

        /// <summary>
        /// Function to check if the board is loaded
        /// </summary>
        /// <returns></returns>
        public bool isBoardLoaded()
        {
            return CameraController.HasInstance && BoardSessionManager.HasInstance && !BoardSessionManager.IsLoading;
        }

        const string taleSpireIcoBase64 = "AAABAAEAQEAAAAEAIAAoQgAAFgAAACgAAABAAAAAgAAAAAEAIAAAAAAAAEAAABMLAAATCwAAAAAAAAAAAABAQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+TE5O/kNDQ/5BQkL/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEFB/mhvcf5YXV7+QUFB/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5iaGr+kZ+i/kZHR/5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+XGBh/qGytf5kamz+QUJC/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/k1PT/6ktrn+lqWo/klLS/5AQUH/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5BQkL+obK1/q/Cxv5qcnT+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/pGgov6yxsr+na2x/klKS/5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP54goT+ssbK/q7Cxv5ud3n+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+YGZo/rHFyf6yxsr+oLG1/k1QUP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/lVYWf6rvcH+ssbK/rLGyv6EkJL+QkJC/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5MT0/+nKyv/rLGyv6yxsr+qby//mNqa/5AQUH+QEFB/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+R0hI/oqYmv6yxsn+ssbK/rHFyf6Yp6r+UVRV/kBAQP5CQ0P/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kFCQv50fX/+r8LG/rLGyv6yxsr+scTI/oiVl/5FR0f+QEFB/kFBQf9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+Y2lq/qq9wf6yxsr+ssbK/rLGyv6sv8P+dH1+/kJDQ/5BQkL+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/k1QUP6is7f+ssbK/rLGyv6yxsr+ssbK/qq9wf5xeXv+QkJC/kJDQ/5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5BQUH+jpyf/rLGyv6yxsr+ssbK/rLGyv6yxsr+q73B/mx0df5ERkb+QkRE/kFCQv9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/mNpa/6vw8f+ssbK/rLGyv6yxsr+ssbK/rLGyv6ou77+f4qM/klLTP5CQkP+REVF/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QUFB/kRFRf5GR0f+RkhI/kNERP5BQUH+QEFB/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5NUFD+nq+y/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rDDx/6LmZv+YWdo/kRERP5BQkL/QUFB/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5BQUH+TE5O/mFnaP52gIH+go2Q/oOPkf54g4X+ZGpr/kdISP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+RkdH/omWmf6xxcn+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+sMTI/qS2uf5/ioz+Wl9g/kVFRf5BQkL/QUFB/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kVGRv5PUVL+bnZ4/o+cn/6mt7v+rcDE/q/Cxv6vw8f+rcDE/qe5vf6GkpX+R0hI/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kFBQf5udXf+rcDE/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+sMTI/qS2uf6Hk5b+anBy/lNXV/5HSEj+QEFB/kBAQP5AQED+QEBA/kBBQf5FRkf+U1ZX/mJpav5/i43+mqqt/qu+wv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+sMPH/n6KjP5BQkL/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+SkxM/purrv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+sMPH/qy/w/6fsLP+laOn/oaSlf5+iYz+fIaI/n6Ii/6GkpX+kZ6h/pytsP6our7+r8LG/rHFyf6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6pu7/+TE9P/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5weHn+scXJ/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6xxcn+scXJ/rHFyf6xxcn+scXJ/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6ww8f+kqGk/ktNTf5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+UlVW/qK0t/6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rDEyP6ltrr+g4+S/lFUVf5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kNERP56hYf+r8LG/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/q7Bxf6Klpn+W2Bh/kJDQ/5AQED+QEBA/kBAQP5AQED+QEFB/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEFB/lFTVP5VWVr+TlFR/kVGRv5BQUH+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+U1ZX/p6usv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rDEyP6Nmp3+T1JS/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/khKSv6Uo6b+oLG1/pioq/6HlJf+ZWts/kpMTf5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5tdXf+r8PH/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6gsbT+Vlpa/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5OUFH+qrzA/rLGyv6yxcn+r8PH/qa4u/6Kl5n+ZWtt/klKS/5AQUH+QEBA/kBAQP5AQED+TE9P/pioqv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+eIKE/kJCQv5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+TE5P/qe6vf6yxsr+ssbK/rLGyv6yxsr+scXJ/qy+wv6Xpqn+ZGps/kNDQ/5AQED+QEBA/kFBQf5iaGr+qLq+/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+n6+z/k5RUv5AQED+QkND/kJCQv5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kVGR/6crK/+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+scXJ/qe5vf55g4X+SkxM/kBAQP5AQED+Q0RE/oCLjf6xxcn+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+r8PH/nmDhf5KTEz+doCB/omWmP6HlJb+aXBy/kJDQ/5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+iJWX/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/palqP5aX2D+Q0RE/kBAQP5OUVH+na2w/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/qe5vf5dY2T+Zm5v/q/Dx/6yxsr+ssbK/q3AxP5ob3H+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/m52eP6xxcn+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6xxMj+nq+y/mlxcv5FR0f+QUJC/mtzdf6pvMD+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6fr7L+TlFR/pCeof6yxsr+ssbK/rLGyv6yxsr+gIuN/kFCQv5AQED+QEBA/kBAQP5AQED+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5aXl/+qrzA/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6sv8P+hZGU/lRXWP5DRET+eoSH/rHFyP6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ipea/l5jZP6pvL/+ssbK/rLGyv6yxsr+r8PH/nN8fv5AQUH+QEBA/kBAQP5AQED+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+TE5O/palqP6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/q7Cxv6Rn6L+W2Bh/lBTVP6Xp6r+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/m10dv5zfX7+r8PG/rLGyv6yxsr+ssbK/qCxtf5TVlf+QEBA/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kFBQf5xenz+r8LG/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+scXJ/qW3uv50fX/+YGVm/pysr/6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rDDx/5ZXV7+gY2P/rHFyf6yxsr+ssbK/qq8wP5ocHH+QEFB/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+VVla/qe5vf6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+qbzA/oSQkv5weHr+r8PH/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6our7+U1ZX/oWRlP6xxcn+ssbK/qy/wv51foD+R0hI/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kJDQ/6Rn6P+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6xxMj+mqmt/omWmP6rvsH+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+oLC0/lFUVP6IlJf+scXJ/qe5vf50fX7+QkND/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+ZWts/q7Cxf6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6nub3+nKyv/rHFyf6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/pqqrf5QU1P+iZaZ/qK0t/5qcnP+Q0RE/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kZISP6GkpX+scXI/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rDEyP6twcX+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6YqKv+TlBR/m52eP5aX2D+QUFB/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQUH+X2Rl/qi6vv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+na2w/ktNTf5FRkb+QUFB/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kJCQv53gYL+rMDD/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/qO1uP5MTk7+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+SkxM/oKOkf6vw8b+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6sv8P+UFNU/kBAQP5AQED+QEBA/kBAQP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5JS0v+f4qN/qm8wP6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssXJ/l1iY/5AQED+QEBA/kBAQP5AQED+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kZHR/5rc3X+pbe6/rHFyf6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv58h4n+QEBA/kBAQP5AQED+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kZHSP5VWVr+UVRV/kVFRv5CQkL+QkJC/ldbXP6CjY/+pri8/rDEyP6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+mKer/kRERf5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kZHSP5/ioz+n7Cz/pmorP6BjY/+Y2lq/khKSv5AQED+QEBA/mRrbP6PnJ/+pba6/rHFyP6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/qO0uP5UWFj+QEBA/kBAQP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5gZmf+q73B/rLGyv6yxsr+sMTI/qe6vf6IlZj+VVla/kRFRf5BQkL+SktM/l5kZf59iIr+mquu/qq8wP6vwsb+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6rvsL+aG9w/kBAQP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+d4GC/rDEyP6yxsr+ssbK/rLGyv6yxsr+scXJ/pmprP52gIL+U1dX/kBAQP5AQED+QkND/lRYWf5rc3X+gYyP/pamqf6our7+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+scXJ/oOOkf5EREX+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/m11dv6uwsX+ssbK/rLGyv6yxsr+ssbK/rLGyv6xxcn+rcDE/pinqv5ud3j+Vlpa/kpLTP5GRkf+RkdI/kpMTP5WWlr+ZGtt/omWmP6sv8P+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6muLz+UVRU/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5WWlv+p7q+/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+rL/C/qGytv6RoKL+g46R/n2Iiv6Ai43+hZKU/mtzdf5MTk7+hpKV/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/nF6e/5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+Q0RE/pCfov6yxcn+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+scXJ/rDDx/6vw8f+sMPH/rDEyP6muLz+X2Rl/k5QUf6VpKf+scXJ/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6rvsL+qr3B/rHEyP6yxsr+ssbK/rLGyv6VpKf+RUZG/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5WWlr+na6x/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/p6usf5YXF3+V1tc/pyssP6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6jtbj+aG9w/mFnaP6Nm57+scXJ/rLGyv6yxsr+q77C/mJoaf5DQ0P/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QkJC/mVsbv6ltrr+scXJ/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6wxMj+kZ+i/kxOTv5jaWv+qby//rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6xxcn+hZGU/kRFRf5AQED+V1tc/qa3u/6yxsr+ssbK/rHFyf6EkJP+RkdI/0BBQf9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5CQkP+aXBy/p6vsv6xxcn+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rHFyf5/io3+SUpK/nV+gf6qvcH+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/pOjpv5JS0z+QEBA/ktNTf6VpKf+ssbK/rLGyv6yxsr+p7q9/lRXWP5DRET/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kNERP5jaWr+l6ao/q7Bxf6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+qbu//nN7ff5LTE3+gY2P/rDEyP6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6vwsb+eIOF/k9RUv5TV1f+nKyv/rLGyv6yxsr+ssbK/rLGyv56hYf+QEFB/kFCQv9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QUFB/lFTVP52f4H+orO3/rDEyP6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLFyf6vwsb+qr3A/p2tsf6IlZf+QEBA/khKS/6QnqH+scTI/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/q3AxP6aqq3+mKir/q/Cxv6yxsr+ssbK/rLGyv6yxsr+jZud/kBAQP5BQkL/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+RUVF/l5jZP55goX+jJqc/purrv6jtLj+pbe6/qS1uP6errH+lKOm/ouYmv5/ioz+cXp7/mVsbv5WWlv+SktM/kBAQP5AQED+WV5f/pqqrv6xxcn+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+scXJ/rHFyf6yxsr+ssbK/rLGyv6yxsr+ssbK/oaSlf5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QkJC/kVFRf5GSEj+SElJ/khJSf5ISUn+R0hI/kZHR/5ERUX+Q0ND/kFCQv5AQED+QEBA/kBAQP5AQED+QEBA/kFBQf5VWVr+orO3/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/q/Cxv5iaGn+QEBA/kBAQP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QUFB/mdub/6lt7v+scXJ/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rLGyv6yxsr+ssbK/rDDx/6Kl5n+SElJ/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+XmJk/pSipP6nuLz+rcHF/rDEyP6xxcn+scXJ/rHFyf6wxMj+rcDE/qW2uv6Dj5H+TlFS/kBAQP5AQED+QEBA/kBAQP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kFBQf5PUlL+Y2lq/nyHif6IlZf+jZue/pCeof6Om57+iZWY/nqFhv5fZWX+SUpK/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kJCQv5ERUX+QkND/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/kBAQP5AQED+QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/QEBA/0BAQP9AQED/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    }
}
