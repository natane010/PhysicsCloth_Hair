using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TK.Mono
{
    [RequireComponent(typeof(PCHRuntimeController))]
    public class PCHPhysicsSettingSwitcher : MonoBehaviour
    {
        [SerializeField]
        private PCHRuntimeController runtimeController;
        [SerializeField]
        public PCHSettingLinker currentLinker;
        [SerializeField]
        public List<PCHSettingLinker> targetLinkers =new List<PCHSettingLinker>();
        int index = 0;
        public void Awake()
        {
            runtimeController= gameObject.GetComponent<PCHRuntimeController>(); 
        }
        public void Switch()
        {
            if (targetLinkers==null&&targetLinkers.Count==0)
            {
                return;
            }

            currentLinker = targetLinkers[index];
            for (int i = 0; i < runtimeController.allChain.Length; i++)
            {
                PCHChainProcessor chain = runtimeController.allChain[i];
                string keyword = chain.keyWord;
                PCHPhysicsSetting setting = currentLinker.GetSetting(keyword);
                chain.SetPCHSetting(setting);
            }
            runtimeController.ResetData();

            index = index + 1 <targetLinkers.Count ? index + 1 : 0;
            
        }
    }
}