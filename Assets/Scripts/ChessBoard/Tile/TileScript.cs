using UnityEngine;

using TMPro; // Dùng TextMeshPro



public class TileScript : MonoBehaviour

{

    public GameObject highlight;

    private GameObject _tileHighlight;

    private TileManager _tileManager;

    public int TilePlacement { get; private set; }



    // Biến TextMeshPro (Kéo thả trong Inspector Prefab)

    public TMP_Text CooldownText;



    private Color _originalColor; // Biến lưu màu gốc

    private bool _hasSavedColor = false;



    public void HighlightTile()

    {

        _tileHighlight.SetActive(true);

    }



    public void UnHighlightTile()

    {

        _tileHighlight.SetActive(false);

    }



    void Start()

    {

        _tileManager = gameObject.GetComponentInParent<TileManager>();

        _tileHighlight = transform.Find("TileHighlight").gameObject;



        Vector3 localPosition = transform.localPosition;

        Vector3 localScale = transform.localScale;

        TilePlacement = (int)localPosition.z / (int)(10 * localScale.z) * 8 + (int)localPosition.x / (int)(10 * localScale.x);



        // Tự động lưu màu ngay khi bắt đầu

        SaveOriginalColor();

        // Ẩn text ban đầu

        SetCooldownNumber(0);

    }



    // Hàm lưu màu gốc (Trắng/Đen)

    public void SaveOriginalColor()

    {

        if (!_hasSavedColor)

        {

            var renderer = GetComponent<MeshRenderer>();

            if (renderer != null)

            {

                _originalColor = renderer.material.color;

                _hasSavedColor = true;

            }

        }

    }



    // Hàm hiển thị số đếm ngược

    public void SetCooldownNumber(int turns)

    {

        if (CooldownText == null) return;



        if (turns > 0)

        {

            CooldownText.gameObject.SetActive(true);

            CooldownText.text = turns.ToString();

        }

        else

        {

            CooldownText.gameObject.SetActive(false);

        }

    }



    // Hàm đổi màu (Hỗ trợ 0: Gốc, 1: Vàng, 2: Đỏ)

    public void SetBrokenVisual(int state)

    {

        var renderer = GetComponent<MeshRenderer>();

        if (renderer == null) return;



        // Lưu màu gốc nếu chưa lưu

        if (!_hasSavedColor) SaveOriginalColor();



        switch (state)

        {

            case 2: // Hỏng hoàn toàn (Đỏ)

                renderer.material.color = Color.red;

                // Nền đỏ -> Chữ trắng cho nổi

                if (CooldownText != null) CooldownText.color = Color.white;

                break;



            case 1: // Sắp hỏng (Vàng)

                renderer.material.color = Color.yellow;

                // Nền vàng -> Chữ đen mới đọc được

                if (CooldownText != null) CooldownText.color = Color.black;

                break;



            default: // Bình thường

                renderer.material.color = _originalColor;

                // Reset về màu trắng hoặc màu mặc định

                if (CooldownText != null) CooldownText.color = Color.white;

                break;

        }

    }



    private void OnMouseOver()

    {

        if (Input.GetMouseButtonDown(0))

        {

            _tileManager.clickTile(TilePlacement);

        }

    }



    private void OnMouseEnter()

    {

        if (!BoardManager._humainPlayer) return;

        highlight.SetActive(true);

        highlight.transform.position = transform.position;

    }



    private void OnMouseExit()

    {

        highlight.SetActive(false);

    }

}