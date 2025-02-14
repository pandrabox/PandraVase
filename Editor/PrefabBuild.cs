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


namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PVPrefabBuildC
    {
        [MenuItem("PanDbg/< PrefabBuild Test>")]
        public static void PVPrefabBuildTest()
        {
            new PVPrefabBuildMain(BuildMode.Test);
        }
        [MenuItem("PanDbg/< PrefabBuild Upload>")]
        public static void PVPrefabBuildUpload()
        {
            new PVPrefabBuildMain(BuildMode.Upload);
        }
    }

    public enum BuildMode
    {
        Test,Upload
    }

    public class PVPrefabBuildMain
    {
        const string PREFABPATH = "Assets/Pan/DevTool/PrefabBuild/FlatsPlus.prefab";
        BuildMode _buildMode;
        public PVPrefabBuildMain(BuildMode buildMode)
        {
            ClearConsole();
            Debug.LogWarning($@"PrefabBuild {buildMode.ToString()} Start");
            _buildMode = buildMode;
            Run();
        }

        private async void Run()　//コンストラクタでawaitできないのでメソッドに分ける
        {
            using (UploadAvatar uploadAvatar = new UploadAvatar(PREFABPATH))
            {
                ActivateSDKPanel();
                await UploadAvatarAsync(uploadAvatar);
            }
        }

        private void ActivateSDKPanel() //SDKパネルをアクティブにする（しないとビルドできない）
        {
            EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");
        }

        private async Task UploadAvatarAsync(UploadAvatar uploadAvatar) // 実作業
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
                vrcAvatar = await VRCApi.GetAvatar(uploadAvatar.BlueprintId, true);
                Debug.Log("アバターをビルドします");
                await builder.BuildAndUpload(uploadAvatar.Avatar, vrcAvatar, vrcAvatar.ThumbnailImageUrl);
                //await builder.BuildAndTest(avatar); //Testならこちら
                Debug.Log("アバターのビルドとアップロードが完了しました。");
            }
            catch (Exception e)
            {
                Debug.LogError("アバターのビルドとテスト中にエラーが発生しました: " + e.Message);
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
            void OnBuildError(object sender, string error) => Debug.LogError($"ビルドエラー: {error}");
            void OnSdkUploadStateChange(object sender, SdkUploadState sbs) => Debug.Log($@"アップロードモード:{sbs}");
            void OnUploadStart(object sender, object e) => Debug.Log("アップロード開始");
            void OnUploadProgress(object sender, (string status, float percentage) progress) => Debug.Log($"アップロード進行中: {progress.status}, {progress.percentage * 100}%");
            void OnUploadFinish(object sender, string result) => Debug.Log($"アップロード終了: {result}");
            void OnUploadSuccess(object sender, string result) => Debug.Log($"アップロード成功: {result}");
            void OnUploadError(object sender, string error) => Debug.LogError($"アップロードエラー: {error}");
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

        /// <summary>
        /// プレハブのインスタンシング、アップロード必要情報の取得、破棄
        /// </summary>
        public class UploadAvatar : IDisposable
        {
            public GameObject Avatar { get; }
            public string BlueprintId { get; }
            public UploadAvatar(string prefabPath)
            {
                Avatar = GameObject.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
                if (Avatar == null) { EditorUtility.DisplayDialog("エラー", $@"アバターが見つかりません。{prefabPath}にアバターがあるか確認して下さい", "OK"); Dispose(); }
                Avatar.name = Avatar.name.Replace("(Clone)", "");
                PipelineManager pipeline = Avatar.GetComponent<PipelineManager>();
                if (pipeline == null) { EditorUtility.DisplayDialog("エラー", $@"PipelineManagerが見つかりません。{Avatar.name}に正しくアタッチされているか確認して下さい", "OK"); Dispose(); }
                BlueprintId = pipeline?.blueprintId;
                if (BlueprintId == null || BlueprintId.Length==0) { EditorUtility.DisplayDialog("エラー", $@"BlueprintIdの取得に失敗しました", "OK"); Dispose(); }
            }
            public void Dispose()
            {
                GameObject.DestroyImmediate(Avatar);
            }
        }

        /// <summary>
        /// コンソールをクリア
        /// https://baba-s.hatenablog.com/entry/2018/12/05/141500
        /// コガネブログ　baba_s様
        /// </summary>
        static void ClearConsole()
        {
            var type = Assembly
            .GetAssembly(typeof(SceneView))
#if UNITY_2017_1_OR_NEWER
            .GetType("UnityEditor.LogEntries")
#else
            .GetType( "UnityEditorInternal.LogEntries" )
#endif
        ;
            var method = type.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            method.Invoke(null, null);
        }
    }
#endif
}