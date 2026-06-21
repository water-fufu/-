using UnityEngine;
using System.Collections;

/// <summary>
/// 汉诺塔圆盘组件 — 挂载到每个圆盘预制体上
/// 职责：响应鼠标点击、高亮选中状态、执行三段式移动动画
/// </summary>
public class chh_Disk : MonoBehaviour
{
    [Header("圆盘属性")]
    [Tooltip("大小等级：1=最大盘（蓝），2=中盘（绿），3=最小盘（红）")]
    public int sizeLevel = 1;
    // ↑ sizeLevel 越小 = 盘越大。移动合法性：目标顶部盘的 sizeLevel 必须 < 当前盘

    [Tooltip("选中高亮颜色")]
    public Color highlightColor = Color.yellow;

    // ── 私有状态 ──
    private Color originalColor;
    private Renderer diskRenderer;
    private chh_GameManager gameManager;
    private chh_Pillar currentPillar;
    private bool isSelected = false;

    void Start()
    {
        diskRenderer = GetComponent<Renderer>();
        if (diskRenderer != null)
            originalColor = diskRenderer.material.color;

        gameManager = FindObjectOfType<chh_GameManager>();
        if (gameManager == null)
            Debug.LogError("【chh_Disk】找不到 chh_GameManager！请在场景中创建并挂载 chh_GameManager.cs。");
    }

    /// <summary>鼠标点击 → 转发给 GameManager</summary>
    void OnMouseDown()
    {
        if (gameManager != null) gameManager.OnDiskClicked(this);
    }

    /// <summary>高亮选中</summary>
    public void Select()
    {
        if (isSelected) return;
        isSelected = true;
        diskRenderer.material.color = highlightColor;
    }

    /// <summary>取消选中</summary>
    public void Deselect()
    {
        if (!isSelected) return;
        isSelected = false;
        diskRenderer.material.color = originalColor;
    }

    public void SetPillar(chh_Pillar pillar) { currentPillar = pillar; }
    public chh_Pillar GetPillar() { return currentPillar; }

    /// <summary>
    /// 三段式移动动画（协程）：抬升 → 平移 → 下降
    /// 结束后回调 GameManager.OnAnimationComplete()
    /// </summary>
    public IEnumerator AnimateMove(Vector3 targetPosition)
    {
        float liftHeight = 2.0f;
        float liftTime = 0.25f, moveTime = 0.35f, dropTime = 0.25f;

        Vector3 start = transform.position;
        Vector3 lifted = new Vector3(start.x, liftHeight, start.z);
        Vector3 above = new Vector3(targetPosition.x, liftHeight, targetPosition.z);

        // ① 抬升
        for (float t = 0; t < liftTime; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, lifted, t / liftTime);
            yield return null;
        }
        transform.position = lifted;

        // ② 平移
        for (float t = 0; t < moveTime; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(lifted, above, t / moveTime);
            yield return null;
        }
        transform.position = above;

        // ③ 下降
        for (float t = 0; t < dropTime; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(above, targetPosition, t / dropTime);
            yield return null;
        }
        transform.position = targetPosition;

        gameManager.OnAnimationComplete();
    }
}
