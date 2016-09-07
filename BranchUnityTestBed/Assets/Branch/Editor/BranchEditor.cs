﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

[CustomEditor(typeof(Branch))]
public class BranchEditor : Editor {

	private bool isNeedToUpdateIOS = false;
	private bool isNeedToUpdateAndroid = false;

	public override void OnInspectorGUI() {
		GUI.changed = false;

		SerializedObject serializedBranchData = new UnityEditor.SerializedObject(BranchData.Instance);

		SerializedProperty serializedIsDebug = serializedBranchData.FindProperty("simulateFreshInstalls");
		SerializedProperty serializedIsTestMode = serializedBranchData.FindProperty("testMode");

		SerializedProperty serializedTestBranchKey = serializedBranchData.FindProperty("testBranchKey");
		SerializedProperty serializedTestBranchUri = serializedBranchData.FindProperty("testBranchUri");
		SerializedProperty serializedTestAndroidPathPrefix = serializedBranchData.FindProperty("testAndroidPathPrefix");
		SerializedProperty serializedTestCustomDomain = serializedBranchData.FindProperty("testCustomDomain");
		SerializedProperty serializedTestAppLinks = serializedBranchData.FindProperty("testAppLinks");

		SerializedProperty serializedBranchKey = serializedBranchData.FindProperty("liveBranchKey");
		SerializedProperty serializedBranchUri = serializedBranchData.FindProperty("liveBranchUri");
		SerializedProperty serializedAndroidPathPrefix = serializedBranchData.FindProperty("liveAndroidPathPrefix");
		SerializedProperty serializedCustomDomain = serializedBranchData.FindProperty("liveCustomDomain");
		SerializedProperty serializedAppLinks = serializedBranchData.FindProperty("liveAppLinks");


		EditorGUILayout.PropertyField(serializedIsDebug, new GUILayoutOption[]{});
		EditorGUILayout.PropertyField(serializedIsTestMode, new GUILayoutOption[]{});

		GUI.enabled = BranchData.Instance.testMode;

		EditorGUILayout.Separator();
		EditorGUILayout.PropertyField(serializedTestBranchKey, new GUILayoutOption[]{});
		EditorGUILayout.PropertyField(serializedTestBranchUri, new GUILayoutOption[]{});
		EditorGUILayout.PropertyField(serializedTestAndroidPathPrefix, new GUILayoutOption[]{});
		EditorGUILayout.PropertyField(serializedTestCustomDomain, new GUILayoutOption[]{});
		EditorGUILayout.PropertyField(serializedTestAppLinks, true, new GUILayoutOption[]{});

		GUI.enabled = !BranchData.Instance.testMode;

		EditorGUILayout.Separator();
		EditorGUILayout.PropertyField(serializedBranchKey, new GUILayoutOption[]{});
		EditorGUILayout.PropertyField(serializedBranchUri, new GUILayoutOption[]{});
		EditorGUILayout.PropertyField(serializedAndroidPathPrefix, new GUILayoutOption[]{});
		EditorGUILayout.PropertyField(serializedCustomDomain, new GUILayoutOption[]{});
		EditorGUILayout.PropertyField(serializedAppLinks, true, new GUILayoutOption[]{});

		GUI.enabled = true;

		EditorGUILayout.BeginHorizontal(new GUILayoutOption[]{});
		if (isNeedToUpdateIOS) {
			if (GUILayout.Button("Update iOS Wrapper", new GUILayoutOption[]{})) {
				UpdateIOSKey();
				isNeedToUpdateIOS = false;
				GUI.changed = false;
				AssetDatabase.Refresh();
			}
		}

		if (isNeedToUpdateAndroid) {
			if (GUILayout.Button("Update Android Manifest", new GUILayoutOption[]{})) {
				UpdateManifest();
				isNeedToUpdateAndroid = false;
				GUI.changed = false;
				AssetDatabase.Refresh();
			}
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal(new GUILayoutOption[]{});
		EditorGUILayout.HelpBox("Read more about adding your Branch link domains for iOS.\nButton \"Update iOS Wrapper\" updates only Branch Key. You have to add link domains into xcode project manually.", MessageType.Info);
		if (GUILayout.Button("?", new GUILayoutOption[]{GUILayout.Width(20)})) {
			Application.OpenURL("https://dev.branch.io/getting-started/universal-app-links/guide/unity/#add-your-branch-link-domains");
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal(new GUILayoutOption[]{});
		EditorGUILayout.HelpBox("Read more about adding your Branch link domains for Android.\nButton \"Update Android Manifest\" updates manifest in accordance with your settings.", MessageType.Info);
		if (GUILayout.Button("?", new GUILayoutOption[]{GUILayout.Width(20)})) {
			Application.OpenURL("https://dev.branch.io/getting-started/universal-app-links/guide/unity/#add-intent-filter-to-manifest");
		}
		EditorGUILayout.EndHorizontal();

		if (GUI.changed) {
			isNeedToUpdateIOS = true;
			isNeedToUpdateAndroid = true;
			serializedBranchData.ApplyModifiedProperties();
			EditorUtility.SetDirty(BranchData.Instance);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}

	#region UpdateIOSKey

	private void UpdateIOSKey() {
		string iosWrapperPath = Path.Combine(Application.dataPath, "Plugins/Branch/iOS/BranchiOSWrapper.mm");

		if (!File.Exists(iosWrapperPath)) {
			return;
		}

		StreamReader sr = new StreamReader(iosWrapperPath, Encoding.Default);
		string [] lines = sr.ReadToEnd().Split('\n').ToArray();
		sr.Close();

		StreamWriter sw = new StreamWriter(iosWrapperPath, false, Encoding.Default);
		for (int i = 0; i < lines.Length; ++i)
		{
			if (lines[i].Contains("static NSString *_branchKey")) {
				if (BranchData.Instance.testMode) {
					sw.WriteLine("static NSString *_branchKey = @\"" + BranchData.Instance.testBranchKey + "\";");
				}
				else {
					sw.WriteLine("static NSString *_branchKey = @\"" + BranchData.Instance.liveBranchKey + "\";");
				}
			}
			else {
				if ( ! (i == lines.Length - 1 && string.IsNullOrEmpty(lines[i])) ) {
					sw.WriteLine(lines[i]);
				}
			}
		}

		sw.Close();
	}

	#endregion

	#region Manifest update

	private void UpdateManifest() {
		
		string manifestFolder = Path.Combine(Application.dataPath, "Plugins/Android");
		string defaultManifestPath = Path.Combine(Application.dataPath, "Plugins/Branch/Android/AndroidManifest.xml");
		string manifestPath = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
		
		if (!File.Exists(manifestPath)) {
			
			if (!Directory.Exists(manifestFolder)) {
				Directory.CreateDirectory(manifestFolder);
			}
			
			File.Copy(defaultManifestPath, manifestPath);
		}
		
		// Opening android manifest
		XmlDocument xmlDoc = new XmlDocument();
		xmlDoc.Load(manifestPath);

		XmlElement rootElem = xmlDoc.DocumentElement;
		XmlNode appNode = null;
		XmlNode unityActivityNode = null;

		// finding node named "application"
		foreach(XmlNode node in rootElem.ChildNodes) {
			if (node.Name == "application") {
				appNode = node;

				XmlElement appElem = appNode as XmlElement;
				if (!appElem.HasAttribute("android:name")) {
					appElem.SetAttribute("android____name", "io.branch.referral.BranchApp");
				}
				break;
			}
		}

		if (appNode == null) {
			Debug.LogError("Current Android Manifest was broken, it does not contain \"<application>\" node");
			return;
		}

		// finding UnityPlayerActivity node
		foreach(XmlNode node in appNode.ChildNodes) {
			if (unityActivityNode != null) {
				break;
			}

			if (node.Name == "activity") {
				foreach(XmlAttribute attr in node.Attributes) {
					if (attr.Value.Contains("UnityPlayerActivity")) {
						unityActivityNode = node;
						break;
					}
				}
			}
		}

		if (unityActivityNode == null) {
			Debug.LogError("Current Android Manifest was broken, it does not contain \"<activity android:name=\"com.unity3d.player.UnityPlayerActivity\">\"");
			return;
		}

		// Adding intent-filter for Branch URI into UnityPlayerActivity activity
		UpdateURIFilter(xmlDoc, unityActivityNode);

		// Adding intent-filter for link domains into UnityPlayerActivity activity
		UpdateLinkDomainsFilter(xmlDoc, unityActivityNode);

		// Adding permissions
		UpdatePermissions(xmlDoc);

//		// Adding debug mode meta
//		UpdateDebugModeMeta(xmlDoc, unityActivityNode);
		
		// Saving android manifest
		xmlDoc.Save(manifestPath);

		//Changing "android___" to "android:" after changings
		TextReader manifestReader = new StreamReader(manifestPath);
		string content = manifestReader.ReadToEnd();
		manifestReader.Close();

		Regex regex = new Regex("android____");
		content = regex.Replace (content, "android:");

		TextWriter manifestWriter = new StreamWriter(manifestPath);
		manifestWriter.Write(content);
		manifestWriter.Close();
	}
	
	private void UpdateURIFilter(XmlDocument doc, XmlNode unityActivityNode) {
		XmlNode intentFilterNode = null;

		// update or adding intent-filter
		foreach(XmlNode node in unityActivityNode.ChildNodes) {
			if (intentFilterNode != null) {
				break;
			}

			if (node.Name == "intent-filter") {
				foreach(XmlNode childNode in node.ChildNodes) {
					foreach(XmlAttribute attr in childNode.Attributes) {
						if (attr.Name.Contains("host") && attr.Value == "open") {
							intentFilterNode = node;
							break;
						}
					}
				}
			}
		}

		// delete old intent-filter
		if (intentFilterNode != null) {
			unityActivityNode.RemoveChild(intentFilterNode);
		}

		// is URI present?
		if (BranchData.Instance.testMode && string.IsNullOrEmpty(BranchData.Instance.testBranchUri)) {
			return;
		}
		else if (!BranchData.Instance.testMode && string.IsNullOrEmpty(BranchData.Instance.liveBranchUri)) {
			return;
		}

		// <intent-filter>
		//	  <data android:scheme="APP_URI" android:host="open" />
		//	  <action android:name="android.intent.action.VIEW" />
		//	  <category android:name="android.intent.category.DEFAULT" />
		//	  <category android:name="android.intent.category.BROWSABLE" />
		// </intent-filter>

		// adding new intent-filter
		XmlElement ifElem = doc.CreateElement("intent-filter");

		XmlElement ifData = doc.CreateElement("data");
		ifData.SetAttribute("android____host", "open");

		if (BranchData.Instance.testMode) {
			ifData.SetAttribute("android____scheme", BranchData.Instance.testBranchUri);
		}
		else {
			ifData.SetAttribute("android____scheme", BranchData.Instance.liveBranchUri);
		}

		XmlElement ifAction = doc.CreateElement("action");
		ifAction.SetAttribute("android____name", "android.intent.action.VIEW");

		XmlElement ifCategory01 = doc.CreateElement("category");
		ifCategory01.SetAttribute("android____name", "android.intent.category.DEFAULT");

		XmlElement ifCategory02 = doc.CreateElement("category");
		ifCategory02.SetAttribute("android____name", "android.intent.category.BROWSABLE");

		ifElem.AppendChild(ifData);
		ifElem.AppendChild(ifAction);
		ifElem.AppendChild(ifCategory01);
		ifElem.AppendChild(ifCategory02);
		unityActivityNode.AppendChild(ifElem);
	}

	private void UpdateLinkDomainsFilter(XmlDocument doc, XmlNode unityActivityNode) {
		XmlNode intentFilterNode = null;

		// update or adding intent-filter
		foreach(XmlNode node in unityActivityNode.ChildNodes) {
			if (intentFilterNode != null) {
				break;
			}

			if (node.Name == "intent-filter") {
				foreach(XmlNode childNode in node.ChildNodes) {
					foreach(XmlAttribute attr in childNode.Attributes) {
						if (attr.Name.Contains("scheme") && attr.Value == "https") {
							intentFilterNode = node;
						}
					}
				}
			}
		}

		if (intentFilterNode != null) {
			unityActivityNode.RemoveChild(intentFilterNode);
		}

//		<intent-filter android:autoVerify="true">
//			<action android:name="android.intent.action.VIEW" />
//			<category android:name="android.intent.category.DEFAULT" />
//			<category android:name="android.intent.category.BROWSABLE" />
//			<data android:scheme="https" android:host="xxxx.app.link" />
//			<data android:scheme="https" android:host="bnc.lt" android:pathPrefix="/pref" />
//			<data android:scheme="https" android:host="custom.dom" android:pathPrefix="/pref" />
//		</intent-filter>

		// adding intent-filter
		XmlElement ifElem = doc.CreateElement("intent-filter");
		ifElem.SetAttribute("android____autoVerify", "true");

		XmlElement ifAction = doc.CreateElement("action");
		ifAction.SetAttribute("android____name", "android.intent.action.VIEW");

		XmlElement ifCategory01 = doc.CreateElement("category");
		ifCategory01.SetAttribute("android____name", "android.intent.category.DEFAULT");

		XmlElement ifCategory02 = doc.CreateElement("category");
		ifCategory02.SetAttribute("android____name", "android.intent.category.BROWSABLE");

		ifElem.AppendChild(ifAction);
		ifElem.AppendChild(ifCategory01);
		ifElem.AppendChild(ifCategory02);

		if (BranchData.Instance.testMode) {
			if (BranchData.Instance.testAppLinks.Length > 0) {
				foreach(string link in BranchData.Instance.testAppLinks) {
					XmlElement ifData = doc.CreateElement("data");
					ifData.SetAttribute("android____scheme", "https");
					ifData.SetAttribute("android____host", link);
					ifElem.AppendChild(ifData);
				}
			}
			else if (!string.IsNullOrEmpty(BranchData.Instance.testCustomDomain)) {
				XmlElement ifData = doc.CreateElement("data");
				ifData.SetAttribute("android____scheme", "https");
				ifData.SetAttribute("android____host", BranchData.Instance.testCustomDomain);
				ifData.SetAttribute("android____pathPrefix", BranchData.Instance.testAndroidPathPrefix);
				ifElem.AppendChild(ifData);
			}
			else {
				XmlElement ifData = doc.CreateElement("data");
				ifData.SetAttribute("android____scheme", "https");
				ifData.SetAttribute("android____host", "bnc.lt");
				ifData.SetAttribute("android____pathPrefix", BranchData.Instance.testAndroidPathPrefix);
				ifElem.AppendChild(ifData);
			}
		}
		else {
			if (BranchData.Instance.liveAppLinks.Length > 0) {
				foreach(string link in BranchData.Instance.liveAppLinks) {
					XmlElement ifData = doc.CreateElement("data");
					ifData.SetAttribute("android____scheme", "https");
					ifData.SetAttribute("android____host", link);
					ifElem.AppendChild(ifData);
				}
			}
			else if (!string.IsNullOrEmpty(BranchData.Instance.liveCustomDomain)) {
				XmlElement ifData = doc.CreateElement("data");
				ifData.SetAttribute("android____scheme", "https");
				ifData.SetAttribute("android____host", BranchData.Instance.liveCustomDomain);
				ifData.SetAttribute("android____pathPrefix", BranchData.Instance.liveAndroidPathPrefix);
				ifElem.AppendChild(ifData);
			}
			else {
				XmlElement ifData = doc.CreateElement("data");
				ifData.SetAttribute("android____scheme", "https");
				ifData.SetAttribute("android____host", "bnc.lt");
				ifData.SetAttribute("android____pathPrefix", BranchData.Instance.liveAndroidPathPrefix);
				ifElem.AppendChild(ifData);
			}
		}

		unityActivityNode.AppendChild(ifElem);
	}

	private void UpdatePermissions(XmlDocument doc) {
		// we have to add the next permissions:
		// <uses-permission android:name="android.permission.INTERNET" />
		// <uses-permission android:name="android.permission.READ_PHONE_STATE" />
		// <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />

		bool isInternetPermission = false;
		bool isReadPhoneState = false;
		bool isAccessNetworkState = false;

		// finding permissions nodes
		XmlElement rootElem = doc.DocumentElement;

		foreach(XmlNode node in rootElem.ChildNodes) {
			if (node.Name == "uses-permission") {
				foreach(XmlAttribute attr in node.Attributes) {
					if (attr.Value.Contains("android.permission.INTERNET")) {
						isInternetPermission = true;
					}
					else if (attr.Value.Contains("android.permission.READ_PHONE_STATE")) {
						isReadPhoneState = true;
					}
					else if (attr.Value.Contains("android.permission.ACCESS_NETWORK_STATE")) {
						isAccessNetworkState = true;
					}
				}
			}
		}

		// adding permissions if need
		// we add "android____name" because there is some troubles to add android:name
		if (!isInternetPermission) {
			XmlElement elem = doc.CreateElement("uses-permission");
			elem.SetAttribute("android____name", "android.permission.INTERNET");
			rootElem.AppendChild(elem);
		}

		if (!isReadPhoneState) {
			XmlElement elem = doc.CreateElement("uses-permission");
			elem.SetAttribute("android____name", "android.permission.READ_PHONE_STATE");
			rootElem.AppendChild(elem);
		}

		if (!isAccessNetworkState) {
			XmlElement elem = doc.CreateElement("uses-permission");
			elem.SetAttribute("android____name", "android.permission.ACCESS_NETWORK_STATE");
			rootElem.AppendChild(elem);
		}
	}


	private void UpdateDebugModeMeta(XmlDocument doc, XmlNode unityActivityNode) {
		//<meta-data android:name="io.branch.sdk.TestMode" android:value="true" />

		XmlNode metaDataNode = null;

		// update or adding intent-filter
		foreach(XmlNode node in unityActivityNode.ChildNodes) {
			if (metaDataNode != null) {
				break;
			}

			if (node.Name == "meta-data") {
				foreach(XmlAttribute attr in node.Attributes) {
					if (attr.Value.Contains("io.branch.sdk.TestMode")) {
						metaDataNode = node;
						break;
					}
				}
			}
		}

		XmlElement debugMetaData = doc.CreateElement("meta-data");
		debugMetaData.SetAttribute("android____name", "io.branch.sdk.TestMode");
		debugMetaData.SetAttribute("android____value", BranchData.Instance.simulateFreshInstalls ? "true" : "false");

		if (metaDataNode == null) {
			unityActivityNode.AppendChild(debugMetaData);
		}
		else {
			unityActivityNode.ReplaceChild(debugMetaData, metaDataNode);
		}
	}

	#endregion
}
