﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx.Async;
using UnityEngine.Networking;
using VRM;
using System.Linq;

namespace Live_V
{
    public class StageDirector : MonoBehaviour
    {
        //Prefab読み込み
        public GameObject MusicPlayer;
        public GameObject MainCameraRig;
        public GameObject[] prefabsNeedsActivation;
        public GameObject[] miscPrefabs;
        public GameObject LipSync;

        //Cameraの場所
        public Transform[] cameraPoints;

        ScreenOverlay screenoverlays;

        //Instatiate後操作用
        GameObject musicPlayer;
        CameraSwitcher mainCameraSwitcher;
        GameObject[] objectsNeedsActivation;
        GameObject VRMAvaterController;
        GameObject LipsSyncContoller;
        VRMImporterContext context;

        private async UniTask Awake()
        {
            //PrefabをInstantiateするよ!!
            musicPlayer = (GameObject)Instantiate(MusicPlayer);

            var cameraRig = (GameObject)Instantiate(MainCameraRig);
            mainCameraSwitcher = cameraRig.GetComponentInChildren<CameraSwitcher>();
            screenoverlays = cameraRig.GetComponentInChildren<ScreenOverlay>();

            objectsNeedsActivation = new GameObject[prefabsNeedsActivation.Length];
            for (var i = 0; i < prefabsNeedsActivation.Length; i++)
                objectsNeedsActivation[i] = (GameObject)Instantiate(prefabsNeedsActivation[i]);

            VRMAvaterController = await LoadVRMAvater();            
            mainCameraSwitcher.GetComponentInChildren<CameraSwitcher>().vrm = VRMAvaterController;
            cameraRig.SetActive(true);

            LipsSyncContoller = (GameObject)Instantiate(LipSync);
            LipsSyncContoller.GetComponent<LipSyncController>().target = VRMAvaterController.GetComponent<VRMBlendShapeProxy>();

            foreach (var p in miscPrefabs)Instantiate(p);

            GetComponent<Animator>().enabled = true;
            
        }

        public async UniTask<GameObject> LoadVRMAvater()
        {
            var path = Application.streamingAssetsPath + "/Avater/model.vrm";
           //var path = VRMLoadUniRx.GetVRMPath();
            byte[] VRMByteData;
            GameObject go;
            Debug.Log("読み込み開始");

            using (var uwr = UnityWebRequest.Get(path))
            {
                await uwr.SendWebRequest();
                VRMByteData = uwr.downloadHandler.data;
            }

            context = new VRMImporterContext();
            context.ParseGlb(VRMByteData);
            await context.LoadAsyncTask();
            go =  context.Root;
            context.ShowMeshes();        

            

            go.AddComponent<Blinker>();
            go.AddComponent<FaceUpdate>();
            var animator = go.GetComponent<Animator>();
            animator.applyRootMotion = true;
            animator.runtimeAnimatorController = (RuntimeAnimatorController)Instantiate(Resources.Load("MocapC86"));
            go.SetLayerRecursively(8);            

            return go;
        }

        public void StartMusic()
        {
            foreach (var source in musicPlayer.GetComponentsInChildren<AudioSource>())
                source.Play();
        }

        public void ActivateProps()
        {
            foreach (var o in objectsNeedsActivation) o.BroadcastMessage("ActivateProps");
        }


        public void SwitchCamera(int index)
        {
            if (mainCameraSwitcher)
                mainCameraSwitcher.ChangePosition(cameraPoints[index], true);
        }

        public void StartAutoCameraChange()
        {
            if (mainCameraSwitcher)
                mainCameraSwitcher.StartAutoChange();
        }

        public void StopAutoCameraChange()
        {
            if (mainCameraSwitcher)
                mainCameraSwitcher.StopAutoChange();
        }

        public void  SwitchOverlays()
        {
            screenoverlays.enabled = true;
        }

        public void FastForward(float second)
        {
            /*if (false)
            {
                FastForwardAnimator(GetComponent<Animator>(), second, 0);
                foreach (var go in objectsOnTimeline)
                    foreach (var animator in go.GetComponentsInChildren<Animator>())
                        FastForwardAnimator(animator, second, 0.5f);
            }*/
        }

        void FastForwardAnimator(Animator animator, float second, float crossfade)
        {
            for (var layer = 0; layer < animator.layerCount; layer++)
            {
                var info = animator.GetCurrentAnimatorStateInfo(layer);
                if (crossfade > 0.0f)
                    animator.CrossFade(info.fullPathHash, crossfade / info.length, layer, info.normalizedTime + second / info.length);
                else
                    animator.Play(info.fullPathHash, layer, info.normalizedTime + second / info.length);
            }
        }

        public void EndPerformance()
        {
            //Application.LoadLevel(0);
            //SceneManager.LoadScene(0);
            screenoverlays.enabled = true;
            context.Dispose();
            Destroy(LipsSyncContoller);
            Destroy(mainCameraSwitcher);
            foreach (var p in objectsNeedsActivation)
                Destroy(p);
        }

    }

    public static class GameObjectExtensions
    {
        /// <summary>
        /// 自分自身を含むすべての子オブジェクトのレイヤーを設定します
        /// </summary>
        public static void SetLayerRecursively(
            this GameObject self,
            int layer
        )
        {
            self.layer = layer;

            foreach (Transform n in self.transform)
            {
                SetLayerRecursively(n.gameObject, layer);
            }
        }
    }
}