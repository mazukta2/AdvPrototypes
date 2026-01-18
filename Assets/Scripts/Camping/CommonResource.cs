using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Camping
{
    public class CommonResource : MonoBehaviour
    {
        public int MinRandomize;
        public int MaxRandomize;
        
        public int Count;
        public float Progress;
        public float ProgressSpeed = 0.1f;
        
        public TextMeshProUGUI Text;
        public Color OutOfRange;
        public Color InRange;
        public Image ProgressImage;
        private CampingResourceBase _commonResource;

        protected void OnEnable()
        {
            Count = UnityEngine.Random.Range(MinRandomize, MaxRandomize);
            _commonResource = GetComponent<CampingResourceBase>();
        }

        public string GetName() => _commonResource.GetName();
        public void TakeResource()=> _commonResource.TakeResource();
        public float GetProgressModificator()=> _commonResource.GetProgressModificator();

        public void Update()
        {
            Text.text = GetName() + " " + Count.ToString();

            if (IsInDistance())
            {
                Text.color = InRange;

                if (PartyCamp.IsPartyCampling() && Count > 0)
                {
                    Progress = Mathf.MoveTowards(Progress, 1, ProgressSpeed * GetProgressModificator() * Time.deltaTime);
                    if (Progress >= 1)
                    {
                        TakeResource();
                        Count--;
                        Progress = 0;
                    }
                }
                else
                {
                    Progress = 0;
                }
            }
            else
            {
                Text.color = OutOfRange;
                Progress = 0;
            }

            ProgressImage.fillAmount = Progress;
        }

        private bool IsInDistance()
        {
            if (PartyCamp.Instance == null)
                return false;
            
            var party = PartyCamp.Instance.transform.position;
            
            return Vector3.Distance(party, transform.position) <= PartyCamp.Instance.Range;
        }
    }
}