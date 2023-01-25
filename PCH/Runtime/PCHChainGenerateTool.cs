using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TK.Mono.Tool
{
    public enum ChainGeneratorMode
    {
         DynamicBone,
         Chain,
         Clear
    }
    public class PCHChainGenerateTool :MonoBehaviour
    {
        [SerializeField]
        public List<string> generateKeyWordWhiteList = new List<string> {};// "hair", "tail", 
        [SerializeField]
        public List<string> generateKeyWordBlackList = new List<string> { "ik","mesh" };
        [SerializeField]
        public List<Transform> blackListOfGenerateTransform = new List<Transform>();
        [SerializeField]
        public List<Transform> generateTransformList = new List<Transform>() {};
        [SerializeField]
        public PCHSettingLinker linker;
        [SerializeField]
        public PCHPhysicsSetting setting;
        [SerializeField]
        public List<PCHChainProcessor> allChain;
        [SerializeField]
        public bool isGenerateOnStart;
        [SerializeField]
        public ChainGeneratorMode generatorMode;

        private void ListCheck()
        {
            if (allChain == null)
            {
                allChain = new List<PCHChainProcessor>();
            }
            for (int i = 0; i < allChain.Count; i++)
            {
                if (allChain[i]==null)
                {
                    allChain.RemoveAt(i);
                    i--;
                }
            }
            if (generatorMode != ChainGeneratorMode.Chain)
            {
                return;
            }
            if (generateKeyWordWhiteList == null || generateKeyWordWhiteList.Count == 0)
            {
                generateKeyWordWhiteList = linker?.AllKeyWord;
                if (generateKeyWordWhiteList == null)
                {
                    Debug.Log("The white key is null!Check the ChainGenerateTool or Value Setting!");
                    return;
                }
            }
            else
            {
                for (int i = 0; i < generateKeyWordWhiteList.Count; i++)
                {
                    if (!string.IsNullOrEmpty(generateKeyWordWhiteList[i]))
                    {
                        generateKeyWordWhiteList[i] = generateKeyWordWhiteList[i].ToLower();
                    }
                    else
                    {
                        generateKeyWordWhiteList[i] = null;
                    }
                }
            }
            for (int i = 0; i < generateKeyWordBlackList.Count; i++)
            {
                generateKeyWordBlackList[i] = generateKeyWordBlackList[i].ToLower();
            }
            if (linker == null)
            {
                linker = Resources.Load("Setting/DefaultSettingLinker") as PCHSettingLinker;
            }
        }
        public  void ClearBoneChain()
        {
            for (int i = 0; i < allChain?.Count; i++)
            {
                var targetChain = allChain[i];
                for (int j0 = 0; j0 < targetChain.allPointList.Count; j0++)
                {
                    DestroyImmediate(targetChain.allPointList[j0]);
                }
                DestroyImmediate(targetChain);
            }

            if (gameObject.TryGetComponent<PCHRuntimeController>(out PCHRuntimeController target))
            {
                target.ListCheck();
            }
            allChain = null;
        }
            public string GetPointCount()
        {
            int count = 0;
            for (int i = 0; i < allChain?.Count; i++)
            {
                if (allChain[i]!=null&&allChain[i].allPointList!=null)
                {
                    count += allChain[i].allPointList.Count;
                }

            }
            return count.ToString();
        }

        public void InitializeChain()
        {
            ListCheck();
            switch (generatorMode)
            {
                case ChainGeneratorMode.DynamicBone:
                    List<string> everyKey = new List<string>() { "" };
                    var tempLinker = ScriptableObject.CreateInstance<PCHSettingLinker>();
                    tempLinker.settings = new List<KeyWordSetting>() {
                        new KeyWordSetting()
                        {
                            setting=setting,
                            keyWord =everyKey
                        }
                    };
                    if (generateTransformList.Count == 0)
                    {
                        generateTransformList.Add(transform);
                    }
                    for (int i = 0; i < generateTransformList.Count; i++)
                    {
                        if (generateTransformList[i]==null||(!generateTransformList[i].gameObject.GetComponentsInParent<Transform>().Contains(transform)))
                        {
                            continue;
                        }
                        GenerateBoneChainImporter(generateTransformList[i], everyKey, new List<string>(), new List<Transform>(), tempLinker, ref allChain);
                    }
                    break;
                case ChainGeneratorMode.Chain:
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        GenerateBoneChainImporter(transform.GetChild(i), generateKeyWordWhiteList, generateKeyWordBlackList, blackListOfGenerateTransform, linker, ref allChain);
                    }
                    break;
                default:
                    break;
            }
            for (int i = 0; i < allChain.Count; i++)
            {
                allChain[i].Initialize();
            }

        }
        #region Static Generate Func
        public static void ClearBoneChain(Transform transform)
        {
            var boneChainsArray = transform.gameObject.GetComponentsInChildren<PCHChainProcessor>();
            for (int i = 0; i < boneChainsArray.Length; i++)
            {
                Component.DestroyImmediate(boneChainsArray[i]);
            }
            var points = transform.gameObject.GetComponentsInChildren<PCHRuntimePoint>();
            for (int i = 0; i < points.Length; i++)
            {
                Component.DestroyImmediate(points[i]);

            }

        }
        public  PCHChainProcessor[] GetChainsInChildren()
        {
            switch (generatorMode)
            {
                case ChainGeneratorMode.DynamicBone:
                    List<PCHChainProcessor> list = new List<PCHChainProcessor>();
                    for (int i = 0; i < generateTransformList.Count; i++)
                    {
                        var target = generateTransformList[i].parent ?? generateTransformList[i];
                        if (target.TryGetComponent<PCHChainProcessor>(out PCHChainProcessor value))
                        {
                            if (!list.Contains(value))
                            {
                                list.Add(value);
                            }
                        }
                    }
                    return list.ToArray();
                case ChainGeneratorMode.Chain:
                    return gameObject.GetComponentsInChildren<PCHChainProcessor>();
                default:
                    throw new InvalidOperationException();
                    break;
            }

        }
        public static void GenerateBoneChainImporter(Transform transform, List<string> generateKeyWordWhiteList, List<string> generateKeyWordBlackList, List<Transform> blackListOfGenerateTransform, PCHSettingLinker settings,ref List<PCHChainProcessor> chainProcessors)//OYM：一个巨丒嗦的方法
        {
            if (transform == null||
                (transform.TryGetComponent<PCHRuntimePoint>(out PCHRuntimePoint point)&&!point.isRoot)) return ;

            bool isblack = false;
            do
            {
                if (!transform.gameObject.activeInHierarchy|| transform.parent==null)
                {
                    continue;
                }

                for (int j0 = 0; j0 < blackListOfGenerateTransform.Count; j0++)
                {
                    if (isblack) break;
                    isblack = transform.Equals(blackListOfGenerateTransform[j0]);
                }
                if (isblack) return;

                string transformName = transform.name.ToLower();
                for (int j0 = 0; j0 < generateKeyWordBlackList.Count; j0++)
                {
                    if (isblack) break;
                    isblack = transformName.Contains(generateKeyWordBlackList[j0]);

                }
                if (isblack) break;

                for (int i = 0; i < generateKeyWordWhiteList.Count; i++)
                {
                    string whiteKey = generateKeyWordWhiteList[i];
                    if (whiteKey == null) continue;

                    if (transformName.Contains(whiteKey))
                    {
                       var fixedNode =  SearchPCHRuntimePoint(transform, new List<string>() { whiteKey }, generateKeyWordBlackList, blackListOfGenerateTransform, 0);
                        var parent = transform.parent;
                        PCHChainProcessor chainProcessor= chainProcessors.FirstOrDefault(x => x.CanMerge(fixedNode, settings.GetSetting(whiteKey)));
                        if (chainProcessor == null)
                        {
                            chainProcessor = PCHChainProcessor.CreatePCHChainProcessor(parent, whiteKey, settings.GetSetting(whiteKey));
                            chainProcessors.Add(chainProcessor);
                        }
                        chainProcessor.AddChild(fixedNode);
                    }
                }
            } while (false);

            for (int i = 0; i < transform.childCount; i++)
            {
                GenerateBoneChainImporter(transform.GetChild(i), generateKeyWordWhiteList, generateKeyWordBlackList, blackListOfGenerateTransform, settings,ref chainProcessors);
            }
        }

        private static PCHRuntimePoint SearchPCHRuntimePoint(Transform transform, List<string> generateKeyWordWhiteList, List<string> generateKeyWordBlackList, List<Transform> blackListOfGenerateTransform, int depth)
        {       

            PCHRuntimePoint point = null;
            do
            {
                if (transform == null) break;

                if (!transform.gameObject.activeInHierarchy) break;
                bool isblack = false;

                for (int i = 0; i < blackListOfGenerateTransform.Count; i++)
                {
                    if (isblack) break;
                    isblack = transform.Equals(blackListOfGenerateTransform[i]);
                }
                string transformName = transform.name.ToLower();
                for (int i = 0; i < generateKeyWordBlackList.Count; i++)
                {
                    if (isblack) break;
                    isblack = transformName.Contains(generateKeyWordBlackList[i]);

                }
                if (isblack) break;

                for (int i = 0; i < generateKeyWordWhiteList.Count; i++)
                {
                    string whiteKey = generateKeyWordWhiteList[i];
                    if (whiteKey == null) continue;
                    if (transformName.Contains(whiteKey))
                    {
                        point =  PCHRuntimePoint.CreateRuntimePoint(transform, depth, whiteKey, !transformName.Contains(PCHChainProcessor.virtualKey));
                        break;
                    }
                }
            } while (false);

            if (point != null)
            {

                for (int i = 0; i < transform.childCount; i++)
                {
                    var childPoint = SearchPCHRuntimePoint(point.transform.GetChild(i), generateKeyWordWhiteList, generateKeyWordBlackList, blackListOfGenerateTransform, depth + 1);
                    if (childPoint != null)
                    {
                        point.AddChild(childPoint);
                    }
                }
            }
            return point;
        }
        #endregion
    }
}
