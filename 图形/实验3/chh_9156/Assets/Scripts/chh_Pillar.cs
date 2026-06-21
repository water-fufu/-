using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 汉诺塔柱子组件 — 挂载到三根柱子上
/// 职责：响应点击、用栈管理圆盘堆叠、计算圆盘落位坐标
/// </summary>
public class chh_Pillar : MonoBehaviour
{
    [Header("柱子标识")]
    [Tooltip("0=左柱(A), 1=中柱(B), 2=右柱(C)")]
    public int pillarIndex = 0;

    [Tooltip("底座顶面 Y 坐标（圆盘起始高度）")]
    public float baseTopY = -0.325f;
    // ↑ 底座 Position(0,-0.5,0) Scale(8,0.2,3) → 顶面 Y = -0.5 + 0.1 = -0.4
    //   第一个圆盘半高 0.075 → -0.4 + 0.075 = -0.325

    [Tooltip("圆盘厚度")]
    public float diskHeight = 0.15f;

    private Stack<chh_Disk> diskStack = new Stack<chh_Disk>();
    // ↑ 栈顶 = 柱子最上方圆盘；Push 入栈 / Pop 出栈 / Peek 看一眼
    private chh_GameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<chh_GameManager>();
        if (gameManager == null)
            Debug.LogError("【chh_Pillar】找不到 chh_GameManager！");
    }

    /// <summary>鼠标点击 → 转发给 GameManager</summary>
    void OnMouseDown()
    {
        if (gameManager != null) gameManager.OnPillarClicked(this);
    }

    // ── 堆栈操作 ──
    public void PushDisk(chh_Disk disk) { diskStack.Push(disk); }
    public chh_Disk PopDisk() { return diskStack.Count > 0 ? diskStack.Pop() : null; }
    public chh_Disk PeekTop() { return diskStack.Count > 0 ? diskStack.Peek() : null; }
    public int GetDiskCount() { return diskStack.Count; }
    public void ClearDisks() { diskStack.Clear(); }

    /// <summary>
    /// 计算下一个圆盘放置的世界坐标（Y 根据已有盘数自动递增）
    /// </summary>
    public Vector3 GetNextDiskPosition()
    {
        float x = transform.position.x;
        float z = transform.position.z;
        float y = baseTopY + diskStack.Count * diskHeight;
        return new Vector3(x, y, z);
    }
}
