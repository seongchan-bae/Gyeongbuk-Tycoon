    using UnityEngine;

    public class Building : MonoBehaviour
    {
        
        [SerializeField]
        public string buildingName;         //상점에서 선택 시 출력될 이름

        [SerializeField]
        public int price;                   //비용 상점에서 사용할 변수
        //public int popular;               //인기도 쓸지는 모르겠음 상점에서 사용하는 변수

        public GameObject prefab;           //
        [SerializeField]
        //건물의 가로 타일
       public int tileWidth = 0 ;

        //건물의 세로 타일
        [SerializeField]
        public int tileHeight = 0;


        [SerializeField]
        private float goldProductionRate = 0f;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }
        public void EarnMoney()
        {
            
        }
        // Update is called once per frame
        void Update()
        {
            
        }
    }
