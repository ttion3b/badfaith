using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Valeur marchande d'un objet lootable. La valeur est PUBLIQUE (étiquette
    /// flottante) : tout le monde sait ce que tu portes — c'est ce qui rend
    /// le braquage et la convoitise possibles.
    /// </summary>
    public class LootItem : NetworkBehaviour
    {
        [SerializeField] private int _minValue = 500;
        [SerializeField] private int _maxValue = 1500;

        private readonly SyncVar<int> _value = new SyncVar<int>();
        private TextMesh _label;

        public int Value => _value.Value;

        public override void OnStartServer()
        {
            base.OnStartServer();
            // Arrondi aux 50 $ : lisible à l'oral ("le mille-deux-cents").
            _value.Value = Mathf.RoundToInt(Random.Range(_minValue, _maxValue) / 50f) * 50;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            var go = new GameObject("PriceTag");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            _label = go.AddComponent<TextMesh>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.GetComponent<MeshRenderer>().material = _label.font.material;
            _label.fontSize = 48;
            _label.characterSize = 0.06f;
            _label.anchor = TextAnchor.MiddleCenter;
            _label.color = new Color(1f, 0.9f, 0.4f);
        }

        private void Update()
        {
            if (_label == null)
                return;
            _label.text = $"{_value.Value} $";
            // Billboard vers la caméra active.
            Camera cam = Camera.main;
            if (cam != null)
                _label.transform.rotation = Quaternion.LookRotation(_label.transform.position - cam.transform.position);
        }
    }
}
