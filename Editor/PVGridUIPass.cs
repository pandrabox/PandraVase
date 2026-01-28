using com.github.pandrabox.pandravase.runtime;
using nadena.dev.ndmf;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.github.pandrabox.pandravase.editor.Util;

namespace com.github.pandrabox.pandravase.editor
{
#if PANDRADBG
    public class PVGridUIDebug
    {
        [MenuItem("PanDbg/PVGridUI/Attach")]
        public static void PVGridUI_Attach()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                PandraProject prj = VaseProject(a);
                prj.SetDebugMode(true);
                prj.SetGridUI("TestGrid", 5, 3, createSampleMenu: true);
            }
        }
        [MenuItem("PanDbg/PVGridUI/Apply")]
        public static void PVGridUI_Apply()
        {
            SetDebugMode(true);
            foreach (var a in AllAvatar)
            {
                new PVGridUIMain(a);
            }
        }
    }
#endif

    internal class PVGridUIPass : Pass<PVGridUIPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            PanProgressBar.Show();
            new PVGridUIMain(ctx.AvatarDescriptor);
        }
    }

    public class PVGridUIMain
    {
        private PVGridUI[] _uis;
        private PandraProject _prj;
        private const string DISPLAYNAME = "Display";
        private PVGridUI _ui;
        private MeshRenderer _dispRenderer;
        private Material _dispMat;
        private int _x;
        private int _y;
        public PVGridUIMain(VRCAvatarDescriptor desc)
        {
            _uis = desc.GetComponentsInChildren<PVGridUI>();
            if (_uis.Length == 0) return;
            _prj = VaseProject(desc);
            foreach (var ui in _uis)
            {
                _ui = ui;
                GetStructure();
                ObjectSetting();
                CreateControl();
            }
        }

        private void GetStructure()
        {
            _dispRenderer = _ui.GetComponentsInChildren<MeshRenderer>(true)?.FirstOrDefault(x => x.name == DISPLAYNAME);
            if (_dispRenderer == null)
            {
                Log.I.Error("Displayが見つかりませんでした。");
                return;
            }
            _dispMat = _dispRenderer.sharedMaterial;
            if (_dispMat == null)
            {
                Log.I.Error("Materialが見つかりませんでした。");
                return;
            }
            if (_ui.MainTex == null)
            {
                Log.I.Error("MainTexが見つかりませんでした。");
                return;
            }
        }
        private void ObjectSetting()
        {
            if (_ui.MainTex != null) _dispMat.mainTexture = _ui.MainTex;
            if (_ui.LockTex != null) _dispMat.SetTexture("_MainTex2", _ui.LockTex);
            _dispMat.SetFloat("_Size", _ui.MenuSize);
            _dispMat.SetFloat("_Opacity", _ui.MenuOpacity);
            _dispMat.SetFloat("_Size2", _ui.LockSize);
            _dispMat.SetColor("_SelectColor", _ui.SelectColor);
            _dispMat.SetFloat("_xMax", _ui.xMax);
            _dispMat.SetFloat("_yMax", _ui.yMax);
        }
        private void CreateControl()
        {
            AnimationClipsBuilder ac = new AnimationClipsBuilder();
            ac.Clip("x1").Bind("Display", typeof(MeshRenderer), "material._x").Const2F(1);
            ac.Clip("y1").Bind("Display", typeof(MeshRenderer), "material._y").Const2F(1);
            ac.Clip("Mode0").Bind("Display", typeof(MeshRenderer), "material._Mode").Const2F(0);
            ac.Clip("Mode1").Bind("Display", typeof(MeshRenderer), "material._Mode").Const2F(1);
            ac.Clip("Disable").Bind("Display", typeof(GameObject), "m_IsActive").Const2F(0)
                .IsVector3((x) => { x.Bind("Display", typeof(Transform), "m_LocalScale.@a").Const2F(0); });
            ac.Clip("Enable").Bind("Display", typeof(GameObject), "m_IsActive").Const2F(1)
                .IsVector3((x) => { x.Bind("Display", typeof(Transform), "m_LocalScale.@a").Const2F(99999); });
            float xSpeed = _ui.Speed;
            float ySpeed = _ui.Speed / _ui.MainTex.width * _ui.MainTex.height;
            BlendTreeBuilder bb = new BlendTreeBuilder("GridUI");
            bb.RootDBT(() =>
            {
                bb.Param("IsLocal").AddD(() =>
                {
                    // ゆっくり移動の計算
                    //Hold
                    bb.Param("1").Add1D(_ui.Reset, () =>
                    {
                        bb.Param(0).AddD(() =>
                        {
                            bb.Param("1").AssignmentBy1D(_ui.Currentx, 0, 1 - xSpeed, _ui.Currentx);
                            bb.Param("1").AssignmentBy1D(_ui.Currenty, 0, 1 - ySpeed, _ui.Currenty);
                        });
                        bb.Param(1).AddD(() =>
                        {
                            bb.Param("1").AddAAP(_ui.Currentx, 0);
                            bb.Param("1").AddAAP(_ui.Currenty, 0);
                        });
                    });
                    //Move
                    bb.Param(_ui.IsEnable).AddD(() =>
                    {
                        bb.Param(_ui.IsMode0).Add1D(_ui.Inputx, () =>
                        {
                            bb.Param(-1).AddAAP(_ui.Currentx, -xSpeed);
                            bb.Param(-_ui.DeadZone).AddAAP(_ui.Currentx, 0);
                            bb.Param(_ui.DeadZone).AddAAP(_ui.Currentx, 0);
                            bb.Param(1).AddAAP(_ui.Currentx, xSpeed);
                        });
                    });
                    bb.Param("1").Add1D(_ui.Inputy, () =>
                    {
                        bb.Param(-1).AddAAP(_ui.Currenty, ySpeed);
                        bb.Param(-_ui.DeadZone).AddAAP(_ui.Currenty, 0);
                        bb.Param(_ui.DeadZone).AddAAP(_ui.Currenty, 0);
                        bb.Param(1).AddAAP(_ui.Currenty, -ySpeed);
                    });
                    // グリッド座標の計算
                    bb.Param("1").Quantization01(_ui.Currentx, _ui.xMax);
                    bb.Param("1").Quantization01(_ui.Currenty, _ui.yMax);
                    //yの制限
                    if (_ui.ItemCount > _ui.xMax && _ui.ItemCount != _ui.xMax * _ui.yMax)
                    {
                        bb.Param("1").Add1D(_ui.Quantizedx, () =>
                        {
                            float threshold = _ui.ItemCount % _ui.xMax;
                            bb.Param(threshold - 0.5f).AssignmentBy1D(_ui.Quantizedy, 0, _ui.yMax, _ui.QuantLimitedy);
                            bb.Param(threshold - 0.4f).AssignmentBy1D(_ui.Quantizedy, 0, _ui.yMax - 2, _ui.QuantLimitedy);
                        });
                    }
                    else
                    {
                        bb.Param("1").AssignmentBy1D(_ui.Quantizedy, 0, _ui.yMax, _ui.QuantLimitedy);
                    }
                    bb.Param(_ui.Quantizedx).AddAAP(_ui.n, 1);
                    bb.Param(_ui.QuantLimitedy).AddAAP(_ui.n, _ui.xMax);
                    // 同期
                    if (_ui.nVirtualSync)
                    {
                        _prj.VirtualSync(_ui.n, TransmissionBit(_ui.xMax * _ui.yMax), PVnBitSync.nBitSyncMode.IntMode);
                    }
                    // アニメーション
                    //bb.Param(_ui.Quantizedx).AddMotion(ac.Outp("x1"));
                    bb.Param("1").FMultiplicationBy1D(ac.Outp("x1"), _ui.Quantizedx, 0, _ui.xMax);
                    bb.Param("1").FMultiplicationBy1D(ac.Outp("y1"), _ui.QuantLimitedy, 0, _ui.yMax);

                    bb.Param("1").Add1D(_ui.IsMode0, () =>
                    {
                        bb.Param(0).AddMotion(ac.Outp("Mode1"));
                        bb.Param(1).AddMotion(ac.Outp("Mode0"));
                    });
                    bb.Param("1").Add1D(_ui.IsEnable, () =>
                    {
                        bb.Param(0).AddMotion(ac.Outp("Disable"));
                        bb.Param(1).AddMotion(ac.Outp("Enable"));
                    });
                });
            });
            bb.Attach(_ui.gameObject);

            if (_ui.CreateSampleMenu)
            {
                var mb = new MenuBuilder(_prj);
                mb.AddFolder("GridUI");
                mb.AddToggle(_ui.IsEnable, "Enable");
                mb.AddToggle(_ui.IsMode0, "Mode1", 0);
                mb.Add2Axis(_ui.Inputx, _ui.Inputy, $@"{_ui.ParameterName}Selecting", "Grid", 0, 0, true);
            }
        }
    }
}
