//コンソールをクリアして規定のプレハブをアップロードする
using UnityEditor;
using UnityEngine;
using System;
using VRC.SDK3A.Editor;
using System.Threading.Tasks;
using VRC.SDKBase.Editor.Api;
using VRC.Core;
using System.Reflection;
using VRC.SDKBase.Editor;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
    public class AvatarBuildMenuDefinition
    {
        [MenuItem("GameObject/Pan/BuildAndTest", false, 0)]
        private static void AvatarBuild_BuildAndTest()
        {
            new AvatarBuild(Selection.activeGameObject, BuildMode.Test);
        }
        [MenuItem("GameObject/Pan/BuildAndUpload", false, 0)]
        private static void AvatarBuild_BuildAndUpload()
        {
            new AvatarBuild(Selection.activeGameObject, BuildMode.Upload);
        }
    }

    public enum BuildMode
    {
        Test,Upload
    }

    public class AvatarBuild
    {

        BuildAvatarTarget _tgt;
        BuildMode _buildMode;
        public AvatarBuild(GameObject tgt, BuildMode buildMode = BuildMode.Upload)
        {
#if PANDRADBG
            //SetDebugMode(true);
#endif
            var tgtDesc = GetAvatarDescriptor(tgt);
            tgtDesc.gameObject.SetActive(true);
            _tgt = new BuildAvatarTarget(tgtDesc.gameObject);
            _buildMode = buildMode;
            ClearConsole();
            Debug.LogWarning($@"AvatarBuilder Build {tgt.name} On Mode {buildMode.ToString()} Start");
            ActivateSDKPanel();
            _ = UploadAvatarAsync(); // 'await' 演算子を適用するため、非同期メソッドの呼び出しを待機します
        }

        public class BuildAvatarTarget
        {
            public GameObject Avatar { get; }
            public string BlueprintId { get; }
            public BuildAvatarTarget(GameObject tgt)
            {
                Avatar = tgt;
                PipelineManager pipeline = tgt.GetComponent<PipelineManager>();
                if (pipeline == null) { EditorUtility.DisplayDialog("エラー", $@"PipelineManagerが見つかりません。{tgt.name}に正しくアタッチされているか確認して下さい", "OK"); return; }
                BlueprintId = pipeline?.blueprintId;
                if (BlueprintId == null || BlueprintId.Length == 0)
                {
                    BlueprintId = GetPanBPID();
                    if (BlueprintId == null || BlueprintId.Length == 0) LowLevelExeption("BlueprintIdの取得に失敗しました Assets/Pan/bpid.txtにBPIDを入れてください");
                    pipeline.blueprintId = BlueprintId;
                }
                if (BlueprintId == null || BlueprintId.Length == 0) { EditorUtility.DisplayDialog("エラー", $@"BlueprintIdの取得に失敗しました", "OK"); return; }
            }

            private string GetPanBPID()
            {
                string path = System.IO.Path.Combine(Application.dataPath, "Pan/bpid.txt");
                if (System.IO.File.Exists(path))
                {
                    return System.IO.File.ReadAllText(path);
                }
                return null;
            }
        }


        private void ActivateSDKPanel() //SDKパネルをアクティブにする（しないとビルドできない）
        {
            EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");
        }

        private async Task UploadAvatarAsync() // 実作業
        {
            if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder))
            {
                Debug.LogError("IVRCSdkAvatarBuilderApiを取得できません。");
                return;
            }
            AddEvents(builder);

            try
            {
                Debug.Log("VRCAvatarを取得します");
                VRCAvatar vrcAvatar = default;
                vrcAvatar = await VRCApi.GetAvatar(_tgt.BlueprintId, true);
                Debug.Log("アバターをビルドします");
                if (_buildMode == BuildMode.Test)
                {
                    await builder.BuildAndTest(_tgt.Avatar);
                }
                else
                {
                    await builder.BuildAndUpload(_tgt.Avatar, vrcAvatar, vrcAvatar.ThumbnailImageUrl);
                }
                PlayClip("Packages/com.github.pandrabox.pandravase/Assets/AvatarBuild/lvup2.mp3");
                Debug.Log("アバターのビルドとアップロードが完了しました。");
            }
            catch (Exception e)
            {
                PlayClip("Packages/com.github.pandrabox.pandravase/Assets/AvatarBuild/chin.mp3");
                Debug.Log("アバターのビルドとテスト中にエラーが発生しました: " + e.Message);
            }
        }

        //進捗表示
        private void AddEvents(IVRCSdkAvatarBuilderApi builder)
        {
            void OnSdkBuildStateChange(object sender, SdkBuildState sbs) => Debug.Log($@"ビルドモード:{sbs}");
            void OnBuildStart(object sender, object e) => Debug.Log($"ビルド開始: {e}");
            void OnBuildProgress(object sender, string e) => Debug.Log($"ビルド進行中: {e}");
            void OnBuildFinish(object sender, string result) => Debug.Log($"ビルド終了: {result}");
            void OnBuildSuccess(object sender, string result) => Debug.Log($"ビルド成功: {result}");
            void OnBuildError(object sender, string error) => Debug.Log($"ビルドエラー: {error}");
            void OnSdkUploadStateChange(object sender, SdkUploadState sbs) => Debug.Log($@"アップロードモード:{sbs}");
            void OnUploadStart(object sender, object e) => Debug.Log("アップロード開始");
            void OnUploadProgress(object sender, (string status, float percentage) progress) => Debug.Log($"アップロード進行中: {progress.status}, {progress.percentage * 100}%");
            void OnUploadFinish(object sender, string result) => Debug.Log($"アップロード終了: {result}");
            void OnUploadSuccess(object sender, string result) => Debug.Log($"アップロード成功: {result}");
            void OnUploadError(object sender, string error) => Debug.Log($"アップロードエラー: {error}");
            builder.OnSdkBuildStateChange -= OnSdkBuildStateChange;
            builder.OnSdkBuildStateChange += OnSdkBuildStateChange;
            builder.OnSdkBuildStart -= OnBuildStart;
            builder.OnSdkBuildStart += OnBuildStart;
            builder.OnSdkBuildProgress -= OnBuildProgress;
            builder.OnSdkBuildProgress += OnBuildProgress;
            builder.OnSdkBuildFinish -= OnBuildFinish;
            builder.OnSdkBuildFinish += OnBuildFinish;
            builder.OnSdkBuildSuccess -= OnBuildSuccess;
            builder.OnSdkBuildSuccess += OnBuildSuccess;
            builder.OnSdkBuildError -= OnBuildError;
            builder.OnSdkBuildError += OnBuildError;
            builder.OnSdkUploadStateChange -= OnSdkUploadStateChange;
            builder.OnSdkUploadStateChange += OnSdkUploadStateChange;
            builder.OnSdkUploadStart -= OnUploadStart;
            builder.OnSdkUploadStart += OnUploadStart;
            builder.OnSdkUploadProgress -= OnUploadProgress;
            builder.OnSdkUploadProgress += OnUploadProgress;
            builder.OnSdkUploadFinish -= OnUploadFinish;
            builder.OnSdkUploadFinish += OnUploadFinish;
            builder.OnSdkUploadSuccess -= OnUploadSuccess;
            builder.OnSdkUploadSuccess += OnUploadSuccess;
            builder.OnSdkUploadError -= OnUploadError;
            builder.OnSdkUploadError += OnUploadError;
        }
    }
}