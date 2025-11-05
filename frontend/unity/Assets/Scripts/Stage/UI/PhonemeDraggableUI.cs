using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public enum PhonemeType { Initial, Medial, Final }

public class PhonemeDraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string symbol;        // "ㅂ", "ㅏ", "ㄹ" 등
    public PhonemeType type;     // 자음/모음 구분용
    public TMP_Text label;

    private CanvasGroup _cg;
    private Transform _originParent;
    private Vector3 _originPos;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (label) label.text = symbol;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originParent = transform.parent;
        _originPos = transform.position;
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _cg.blocksRaycasts = true;
        transform.SetParent(_originParent);
        transform.position = _originPos;
    }
}

