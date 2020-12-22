using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using FMODUnity;
using HarmonyLib;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using static OVRInput;
using Button = UnityEngine.UI.Button;

namespace VREnhancements
{
	[BepInPlugin(GUID, MODNAME, VERSION)]
	public class MainPatcher : BaseUnityPlugin
	{
		public const string
			MODNAME = "VREnhancements",
			AUTHOR = "WhoTnT / Converted to BepInEx by MrPurple6411",
			GUID = "com.whotnt.subnautica.vrenhancements.mod",
			VERSION = "2.0.0.0";


		private static Harmony harmony = new Harmony(GUID);
		private static int tabIndex;
		private static float pdaScale = 1.45f;
		private static float screenScale = 0.0003f;
		private static float defaultZOffset = 0.17f;
		private static float defaultYOffset = 0f;
		private static float pdaXOffset = -0.35f;
		private static float pdaZOffset = 0.28f;
		private static float seaglideZOffset = 0.1f;
		private static float seaglideYOffset = -0.15f;
		private static float swimZOffset = 0.08f;
		private static float swimYOffset = -0.02f;
		private static float pdaXRot = 220f;
		private static float pdaYRot = 30f;
		private static float pdaZRot = 75f;
		private static GameObject quickSlots;
		private static GameObject barsPanel;
		private static Button recenterVRButton;
		private static GameObject leftHandTarget;
		private static FullBodyBipedIK myIK;
		public static bool lookDownHUD = false;
		private static float pdaCloseTimer = 0f;
		private static bool pdaIsClosing = false;
		private static float pdaCloseDelay = 1f;
		private static bool actualGazedBasedCursor;

		public MainPatcher()
		{
		}

		public void Start()
		{
			if (XRSettings.enabled)
			{
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(GameOptions), nameof(GameOptions.GetVrAnimationMode)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("GetVrAnimationMode_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddGeneralTab)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("AddGeneralTab_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(uGUI_TabbedControlsPanel), nameof(uGUI_TabbedControlsPanel.AddTab)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("AddTab_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(GameSettings), nameof(GameSettings.SerializeVRSettings)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("SerializeVRSettings_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(MainCameraControl), nameof(MainCameraControl.Update)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("MCC_Update_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(Vehicle), nameof(Vehicle.OnPilotModeBegin)), new HarmonyMethod(typeof(MainPatcher).GetMethod("OnPilotModeBegin_Prefix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(Vehicle), nameof(Vehicle.OnPilotModeEnd)), new HarmonyMethod(typeof(MainPatcher).GetMethod("OnPilotModeEnd_Prefix")));
#if SN1
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(Subtitles), nameof(Subtitles.Start)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("SubtitlesStart_Postfix")));
#endif
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(PDA), "get_ui"), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("PDA_getui_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(SNCameraRoot), nameof(SNCameraRoot.SetFov)), new HarmonyMethod(typeof(MainPatcher).GetMethod("SNCamSetFov_Prefix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(FPSInputModule), nameof(FPSInputModule.GetCursorScreenPosition)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("GetCursorScreenPosition_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(FPSInputModule), nameof(FPSInputModule.UpdateCursor)), new HarmonyMethod(typeof(MainPatcher).GetMethod("UpdateCursor_Prefix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(FPSInputModule), nameof(FPSInputModule.UpdateCursor)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("UpdateCursor_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(HandReticle), nameof(HandReticle.LateUpdate)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("HandRLateUpdate_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(Player), nameof(Player.Awake)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("Player_Awake_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(uGUI_SceneHUD), nameof(uGUI_SceneHUD.Update)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("SceneHUD_Update_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(SNCameraRoot), nameof(SNCameraRoot.Awake)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("SNCam_Awake_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(uGUI_SceneLoading), nameof(uGUI_SceneLoading.Init)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("Init_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(CyclopsExternalCams), nameof(CyclopsExternalCams.EnterCameraView)), new HarmonyMethod(typeof(MainPatcher).GetMethod("EnterCameraView_Prefix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(uGUI_BuilderMenu), nameof(uGUI_BuilderMenu.Close)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("BuilderMenuClose_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(uGUI_CameraDrone), nameof(uGUI_CameraDrone.Awake)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("CameraDroneAwake_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(uGUI_CameraCyclops), nameof(uGUI_CameraCyclops.Awake)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("CameraCyclopsAwake_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(uGUI_MainMenu), nameof(uGUI_MainMenu.Awake)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("MainMenuAwake_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(PlayerCinematicController), nameof(PlayerCinematicController.SkipCinematic)), new HarmonyMethod(typeof(MainPatcher).GetMethod("SkipCinematic_Prefix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(IngameMenu), nameof(IngameMenu.Awake)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("IGM_Awake_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(MainGameController), nameof(MainGameController.ResetOrientation)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("ResetOrientation_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(PDA), nameof(PDA.Open)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("PDA_Open_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(PDA), nameof(PDA.Close)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("PDA_Close_Postfix")));
#if SN1
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(PDA), nameof(PDA.Update)), new HarmonyMethod(typeof(MainPatcher).GetMethod("PDA_Update_Prefix")));
#elif BZ
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(PDA), nameof(PDA.ManagedUpdate)), new HarmonyMethod(typeof(MainPatcher).GetMethod("PDA_Update_Prefix")));
#endif
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(ArmsController), nameof(ArmsController.Reconfigure)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("Reconfigure_Postfix")));
				MainPatcher.harmony.Patch(AccessTools.Method(typeof(ArmsController), nameof(ArmsController.Start)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("ArmsCon_Start_Postfix")));

				MainPatcher.harmony.Patch(AccessTools.Method(typeof(VROptions), nameof(VROptions.GetUseGazeBasedCursor)), postfix: new HarmonyMethod(typeof(MainPatcher).GetMethod("GetUseGazeBasedCursor_Postfix")));

			}
		}




		public static void GetUseGazeBasedCursor_Postfix(ref bool __result)
		{
			__result = true;
		}

		public static void GetVrAnimationMode_Postfix(ref bool __result)
		{
			__result = !GameOptions.enableVrAnimations;
		}

		public static void AddGeneralTab_Postfix(uGUI_OptionsPanel __instance)
		{
			__instance.AddHeading(MainPatcher.tabIndex, "Additional VR Options");
			__instance.AddToggleOption(MainPatcher.tabIndex, "Enable VR Animations", GameOptions.enableVrAnimations, delegate(bool v)
			{
				GameOptions.enableVrAnimations = v;
				if (Player.main != null)
				{
					Player.main.playerAnimator.SetBool("vr_active", !v);
				}
			});
			__instance.AddToggleOption(MainPatcher.tabIndex, "Look Down for HUD", MainPatcher.lookDownHUD, delegate(bool v)
			{
				MainPatcher.lookDownHUD = v;
				if (!v && MainPatcher.quickSlots != null && MainPatcher.barsPanel != null)
				{
					MainPatcher.quickSlots.transform.localScale = Vector3.one;
					MainPatcher.barsPanel.transform.localScale = Vector3.one;
				}
			});
#if SN1
			__instance.AddSliderOption(MainPatcher.tabIndex, "Walk Speed(Default: 60%)", VROptions.groundMoveScale * 100f, 50f, 100f, 60f, delegate(float v)
			{
				VROptions.groundMoveScale = v / 100f;
			});
#elif BZ
			__instance.AddSliderOption(MainPatcher.tabIndex, "Walk Speed(Default: 60%)", VROptions.groundMoveScale * 100f, 50f, 100f, 60f, 1f, delegate (float v)
			{
				VROptions.groundMoveScale = v / 100f;
			}, SliderLabelMode.Int, "0");
#endif
		}

		public static void AddTab_Postfix(int __result, string label)
		{
			if (label.Equals("General"))
			{
				MainPatcher.tabIndex = __result;
			}
		}

		public static void SerializeVRSettings_Postfix(GameSettings.ISerializer serializer)
		{
			GameOptions.enableVrAnimations = serializer.Serialize("VR/EnableVRAnimations", GameOptions.enableVrAnimations);
			VROptions.groundMoveScale = serializer.Serialize("VR/GroundMoveScale", VROptions.groundMoveScale);
			MainPatcher.lookDownHUD = serializer.Serialize("VR/LookDownHUD", MainPatcher.lookDownHUD);
		}

		public static void MCC_Update_Postfix(MainCameraControl __instance)
		{

			Transform forwardReference = __instance.GetComponentInParent<PlayerController>().forwardReference;
			if (MainPatcher.pdaIsClosing && MainPatcher.pdaCloseTimer < MainPatcher.pdaCloseDelay)
			{
				MainPatcher.pdaCloseTimer += Time.deltaTime;
			}
			else if (MainPatcher.pdaCloseTimer >= MainPatcher.pdaCloseDelay || (MainPatcher.pdaIsClosing && Player.main.GetPDA().state == PDA.State.Opened))
			{
				MainPatcher.pdaIsClosing = false;
				MainPatcher.pdaCloseTimer = 0f;
			}
			if (Player.main.GetPDA().state == PDA.State.Closing)
			{
				MainPatcher.pdaIsClosing = true;
			}
			if (Player.main.GetPDA().state == PDA.State.Closed && !MainPatcher.pdaIsClosing)
			{
				if (Player.main.motorMode == Player.MotorMode.Seaglide)
				{
					__instance.viewModel.transform.localPosition = __instance.viewModel.transform.parent.worldToLocalMatrix.MultiplyPoint(forwardReference.position + forwardReference.up * MainPatcher.seaglideYOffset + forwardReference.forward * MainPatcher.seaglideZOffset);
					return;
				}
#if SN1
				if (Player.main.transform.position.y < Ocean.main.GetOceanLevel() + 1f && !Player.main.IsInside() && !Player.main.precursorOutOfWater)
#elif BZ
				if (Player.main.transform.position.y < Ocean.GetOceanLevel() + 1f && !Player.main.IsInside() && !Player.main.forceWalkMotorMode)
#endif
				{
					string name = Player.main.playerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
					if (name == "Back_lean" || name == "view_surface_swim_forward")
					{
						__instance.viewModel.transform.localPosition = __instance.viewModel.transform.parent.worldToLocalMatrix.MultiplyPoint(forwardReference.position + __instance.viewModel.transform.up * (MainPatcher.swimYOffset - 0.1f) + __instance.viewModel.transform.forward * (MainPatcher.swimZOffset - 0.1f));
						return;
					}
					__instance.viewModel.transform.localPosition = __instance.viewModel.transform.parent.worldToLocalMatrix.MultiplyPoint(forwardReference.position + __instance.viewModel.transform.up * MainPatcher.swimYOffset + __instance.viewModel.transform.forward * MainPatcher.swimZOffset);
					return;
				}
				else if (!__instance.cinematicMode && Player.main.motorMode != Player.MotorMode.Vehicle && Player.main.motorMode != Player.MotorMode.Seaglide)
				{
					if (Player.main.movementSpeed == 0f)
					{
						__instance.viewModel.transform.localPosition = __instance.viewModel.transform.parent.worldToLocalMatrix.MultiplyPoint(forwardReference.position + Vector3.up * MainPatcher.defaultYOffset + new Vector3(forwardReference.forward.x, 0f, forwardReference.forward.z).normalized * MainPatcher.defaultZOffset);
						return;
					}
					__instance.viewModel.transform.localPosition = __instance.viewModel.transform.parent.worldToLocalMatrix.MultiplyPoint(forwardReference.position + Vector3.up * MainPatcher.defaultYOffset + new Vector3(forwardReference.forward.x, 0f, forwardReference.forward.z).normalized * (MainPatcher.defaultZOffset - 0.1f));
				}
			}
		}

		public static bool OnPilotModeBegin_Prefix(Vehicle __instance)
		{
			if (__instance.mainAnimator)
			{
				__instance.mainAnimator.SetBool("vr_active", GameOptions.GetVrAnimationMode());
			}
			return true;
		}

		public static bool OnPilotModeEnd_Prefix(Vehicle __instance)
		{
			if (__instance.mainAnimator)
			{
				__instance.mainAnimator.SetBool("vr_active", GameOptions.GetVrAnimationMode());
			}
			return true;
		}

#if SN1
		public static void SubtitlesStart_Postfix(Subtitles __instance)
		{
			__instance.popup.oy = 800f;
		}
#endif

		public static void PDA_getui_Postfix(PDA __instance)
		{
			uGUI_CanvasScaler component = Traverse.Create(__instance).Field("screen").GetValue<GameObject>().GetComponent<uGUI_CanvasScaler>();
			__instance.transform.localScale = new Vector3(MainPatcher.pdaScale, MainPatcher.pdaScale, 1f);
			component.transform.localScale = Vector3.one * MainPatcher.screenScale;
			component.SetAnchor(__instance.screenAnchor);
		}

		public static bool SNCamSetFov_Prefix()
		{
			return false;
		}

		public static void GetCursorScreenPosition_Postfix(FPSInputModule __instance, ref Vector2 __result)
		{
			if (XRSettings.enabled)
			{
				if (Cursor.lockState == CursorLockMode.Locked)
				{
					__result = GraphicsUtil.GetScreenSize() * 0.5f;
					return;
				}
				if (!MainPatcher.actualGazedBasedCursor)
				{
					__result = new Vector2(Input.mousePosition.x / (float)Screen.width * GraphicsUtil.GetScreenSize().x, Input.mousePosition.y / (float)Screen.height * GraphicsUtil.GetScreenSize().y);
				}
			}
		}

		public static void UpdateCursor_Prefix()
		{
			MainPatcher.actualGazedBasedCursor = VROptions.gazeBasedCursor;
			if (Cursor.lockState != CursorLockMode.Locked)
			{
				VROptions.gazeBasedCursor = true;
			}
		}

		public static void UpdateCursor_Postfix(FPSInputModule __instance)
		{
			VROptions.gazeBasedCursor = MainPatcher.actualGazedBasedCursor;
			Canvas canvas = __instance._cursor.GetComponentInChildren<Graphic>().canvas;
			RaycastResult value = Traverse.Create(__instance).Field("lastRaycastResult").GetValue<RaycastResult>();
			if (canvas && value.isValid)
			{
				canvas.sortingLayerID = value.sortingLayer;
			}
		}

		public static void HandRLateUpdate_Postfix(HandReticle __instance)
		{
			__instance.transform.position = new Vector3(0f, 0f, __instance.transform.position.z);
		}

		public static void Player_Awake_Postfix(uGUI_SceneHUD __instance)
		{
			MainPatcher.barsPanel = GameObject.Find("BarsPanel");
			MainPatcher.quickSlots = GameObject.Find("QuickSlots");
			VROptions.disableInputPitch = false;
		}

		public static void SceneHUD_Update_Postfix(uGUI_SceneHUD __instance)
		{
			if (MainPatcher.lookDownHUD && MainPatcher.quickSlots != null && MainPatcher.barsPanel != null)
			{
				if (Player.main != null && Vector3.Angle(MainCamera.camera.transform.forward, Player.main.transform.up) < 120f)
				{
					MainPatcher.quickSlots.transform.localScale = Vector3.zero;
					MainPatcher.barsPanel.transform.localScale = Vector3.zero;
					return;
				}
				MainPatcher.quickSlots.transform.localScale = Vector3.one;
				MainPatcher.barsPanel.transform.localScale = Vector3.one;
			}
		}

		public static void SNCam_Awake_Postfix(SNCameraRoot __instance)
		{
			GameObject gameObject = __instance.transform.Find("MainCamera").gameObject;
			if (gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(__instance.gameObject.GetComponent<AudioListener>());
				UnityEngine.Object.DestroyImmediate(__instance.gameObject.GetComponent<StudioListener>());
				gameObject.AddComponent<StudioListener>();
			}
		}

		public static void Init_Postfix(uGUI_SceneLoading __instance)
		{
			Image image = null;
			RectTransform rectTransform = null;
			RectTransform rectTransform2 = null;
			try
			{
				image = __instance.loadingBackground.transform.Find("LoadingArtwork").GetComponent<Image>();
				rectTransform = __instance.loadingText.gameObject.GetComponent<RectTransform>();
				rectTransform2 = __instance.loadingBackground.transform.Find("Logo").GetComponent<RectTransform>();
			}
			catch (Exception ex)
			{
				Debug.Log("VR Enhancements Mod: Error finding Loading Screen Elements: " + ex.Message);
				return;
			}
			Vector2 vector = new Vector2(0.5f, 0.5f);
			if (image != null && rectTransform != null && rectTransform2 != null)
			{
				image.sprite = null;
				image.color = Color.black;
				rectTransform2.anchorMin = vector;
				rectTransform2.anchorMax = vector;
				rectTransform2.pivot = vector;
				rectTransform2.anchoredPosition = Vector2.zero;
				rectTransform.anchorMin = vector;
				rectTransform.anchorMax = vector;
				rectTransform.pivot = vector;
				rectTransform.anchoredPosition = new Vector2(0f, -200f);
				rectTransform.sizeDelta = new Vector2(400f, 100f);
				rectTransform.gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
			}
		}

		public static bool EnterCameraView_Prefix(CyclopsExternalCams __instance)
		{
			Traverse.Create(__instance).Field("usingCamera").SetValue(true);
			InputHandlerStack.main.Push(__instance);
			Player main = Player.main;
			MainCameraControl.main.enabled = false;
			Player.main.SetHeadVisible(true);
			__instance.cameraLight.enabled = true;
			Traverse.Create(__instance).Method("ChangeCamera", new object[]
			{
				0
			}).GetValue();
			if (__instance.lightingPanel)
			{
				__instance.lightingPanel.TempTurnOffFloodlights();
			}
			return false;
		}

		public static void BuilderMenuClose_Postfix()
		{
			MainCameraControl.main.ResetLockedVRViewModelAngle();
		}

		public static void CameraDroneAwake_Postfix(uGUI_CameraDrone __instance)
		{
			GameObject gameObject = __instance.transform.Find("Content").Find("CameraScannerRoom").gameObject;
			if (gameObject != null)
			{
				gameObject.GetComponent<RectTransform>().localScale = new Vector3(0.6f, 0.6f, 1f);
				return;
			}
			Debug.Log("VR Enhancements Mod: Cannot set Drone UI scale. Drone Camera Not Found");
		}

		public static void CameraCyclopsAwake_Postfix(uGUI_CameraCyclops __instance)
		{
			GameObject gameObject = __instance.transform.Find("Content").Find("CameraCyclops").gameObject;
			if (gameObject != null)
			{
				gameObject.GetComponent<RectTransform>().localScale = new Vector3(0.7f, 0.7f, 1f);
				return;
			}
			Debug.Log("VR Enhancements Mod: Cannot set CyclopsCamera UI scale. Cyclops Camera Not Found");
		}

		public static void MainMenuAwake_Postfix(uGUI_MainMenu __instance)
		{
			GameObject gameObject = __instance.transform.Find("Panel").Find("MainMenu").gameObject;
			if (gameObject != null)
			{
				gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 385f);
				return;
			}
			Debug.Log("VR Enhancements Mod: Cannot set Main Menu Postions. MainMenu Not Found");
		}

		public static bool SkipCinematic_Prefix(PlayerCinematicController __instance, Player player)
		{
			Traverse.Create(__instance).Field("player").SetValue(player);
			if (player)
			{
				Transform component = player.GetComponent<Transform>();
				Transform component2 = MainCameraControl.main.GetComponent<Transform>();
				if (Traverse.Create(__instance).Method("UseEndTransform", new object[0]).GetValue<bool>())
				{
					player.playerController.SetEnabled(false);
					component.position = __instance.endTransform.position;
					component.rotation = __instance.endTransform.rotation;
					component2.rotation = component.rotation;
				}
				player.playerController.SetEnabled(true);
				player.cinematicModeActive = false;
			}
			if (__instance.informGameObject != null)
			{
				__instance.informGameObject.SendMessage("OnPlayerCinematicModeEnd", __instance, SendMessageOptions.DontRequireReceiver);
			}
			return false;
		}

		public static void IGM_Awake_Postfix(IngameMenu __instance)
		{
			if (__instance != null && MainPatcher.recenterVRButton == null)
			{
				MainPatcher.recenterVRButton = UnityEngine.Object.Instantiate<Button>(__instance.quitToMainMenuButton.transform.parent.GetChild(0).gameObject.GetComponent<Button>(), __instance.quitToMainMenuButton.transform.parent);
				MainPatcher.recenterVRButton.transform.SetSiblingIndex(1);
				MainPatcher.recenterVRButton.name = "RecenterVR";
				MainPatcher.recenterVRButton.onClick.RemoveAllListeners();
				MainPatcher.recenterVRButton.onClick.AddListener(delegate()
				{
					VRUtil.Recenter();
				});
				foreach (Text text in MainPatcher.recenterVRButton.GetComponents<Text>().Concat(MainPatcher.recenterVRButton.GetComponentsInChildren<Text>()))
				{
					text.text = "Recenter VR";
				}
			}
		}

		public static void ResetOrientation_Postfix(MainGameController __instance)
		{
			MainCameraControl.main.cameraOffsetTransform.localPosition = new Vector3(0f, 0f, 0.15f);
		}

		public static void PDA_Open_Postfix(PDA __instance, bool __result)
		{
			if (__result)
			{
				if (!MainPatcher.leftHandTarget)
				{
					MainPatcher.leftHandTarget = new GameObject();
				}
				MainPatcher.leftHandTarget.transform.parent = Player.main.camRoot.transform;
				if (Player.main.motorMode != Player.MotorMode.Vehicle)
				{
					MainPatcher.leftHandTarget.transform.localPosition = MainPatcher.leftHandTarget.transform.parent.transform.InverseTransformPoint(Player.main.playerController.forwardReference.position + Player.main.armsController.transform.right * MainPatcher.pdaXOffset + Vector3.up * -0.15f + new Vector3(Player.main.armsController.transform.forward.x, 0f, Player.main.armsController.transform.forward.z).normalized * MainPatcher.pdaZOffset);
				}
				else
				{
					MainPatcher.leftHandTarget.transform.localPosition = MainPatcher.leftHandTarget.transform.parent.transform.InverseTransformPoint(MainPatcher.leftHandTarget.transform.parent.transform.position + MainPatcher.leftHandTarget.transform.parent.transform.right * MainPatcher.pdaXOffset + MainPatcher.leftHandTarget.transform.parent.transform.forward * MainPatcher.pdaZOffset + MainPatcher.leftHandTarget.transform.parent.transform.up * -0.15f);
				}
				MainPatcher.leftHandTarget.transform.rotation = Player.main.armsController.transform.rotation * Quaternion.Euler(MainPatcher.pdaXRot, MainPatcher.pdaYRot, MainPatcher.pdaZRot);
			}
		}

		public static void PDA_Close_Postfix()
		{
			if (MainPatcher.leftHandTarget)
			{
				UnityEngine.Object.Destroy(MainPatcher.leftHandTarget);
			}
		}

		public static bool PDA_Update_Prefix()
		{
			if (MainPatcher.leftHandTarget)
			{
				MainPatcher.myIK.solver.leftHandEffector.target = MainPatcher.leftHandTarget.transform;
			}
			return true;
		}

		public static void Reconfigure_Postfix(ArmsController __instance)
		{
			Traverse.Create(__instance).Field("reconfigureWorldTarget").SetValue(false);
		}

		public static void ArmsCon_Start_Postfix(ArmsController __instance)
		{
			MainPatcher.myIK = Traverse.Create(__instance).Field("ik").GetValue<FullBodyBipedIK>();
		}


    }
}
