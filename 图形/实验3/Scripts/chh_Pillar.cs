using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 汉诺塔柱子组件 — 挂载到三根柱子上（chh_Pillar_Left / chh_Pillar_Mid / chh_Pillar_Right）
/// 职责：响应鼠标点击、管理该柱子上所有圆盘的堆栈、计算新圆盘的落位坐标
/// </summary>
public class chh_Pillar : MonoBehaviour
{
    // ==================== 公共属性（在 Inspector 中设置） ====================

    [Header("柱子标识")]
    [Tooltip("柱子编号：0=左柱(A), 1=中柱(B), 2=右柱(C)")]
    public int pillarIndex = 0;
    // ↑ 与 GameManager 的 pillars 数组下标对应

    [Tooltip("底座顶面的 Y 坐标（圆盘放置的起始高度）")]
    public float baseTopY = -0.325f;
    // ↑ 底座 Scale(8, 0.2, 3) 在 Position(0, -0.5, 0) 后，
    //   顶面 Y = -0.5 + 0.1 = -0.4，再加上第一个圆盘半高 0.075 = -0.325

    [Tooltip("每个圆盘的高度（厚度），用于计算堆叠偏移")]
    public float diskHeight = 0.15f;
    // ↑ 所有圆盘的 Y 轴 Scale 都是 0.15

    // ==================== 私有变量 ====================

    private Stack<chh_Disk> diskStack = new Stack<chh_Disk>();
    // ↑ 用栈（Stack）数据结构管理圆盘：
    //   栈顶 = 柱子最上方的圆盘；栈底 = 最下方的圆盘
    //   Push：放入圆盘；Pop：取出顶部圆盘；Peek：查看顶部圆盘但不取出

    private chh_GameManager gameManager;
    // ↑ 场景中唯一的游戏管理器引用

    // ==================== Unity 生命周期 ====================

    void Start()
    {
        // 查找游戏管理器
        gameManager = FindObjectOfType<chh_GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("【chh_Pillar】找不到 chh_GameManager！" +
                "请在 Hierarchy 中创建空物体并挂载 chh_GameManager.cs。");
        }
    }

    // ==================== 鼠标交互 ====================

    /// <summary>
    /// 鼠标点击此柱子时，Unity 自动调用
    /// 将事件转发给 GameManager 统一处理
    /// </summary>
    void OnMouseDown()
    {
        if (gameManager != null)
        {
            gameManager.OnPillarClicked(this);
            // ↑ 告诉 GameManager："我（这根柱子）被点击了"
        }
    }

    // ==================== 圆盘堆栈操作 ====================

    /// <summary>
    /// 将一个圆盘放到此柱子顶部
    /// </summary>
    /// <param name="disk">要放入的圆盘</param>
    public void PushDisk(chh_Disk disk)
    {
        diskStack.Push(disk);
        // ↑ 推入栈顶，成为柱子最上方的圆盘
    }

    /// <summary>
    /// 从此柱子顶部取出（移除）最上方的圆盘
    /// </summary>
    /// <returns>被取出的圆盘，如果柱子为空则返回 null</returns>
    public chh_Disk PopDisk()
    {
        if (diskStack.Count > 0)
        {
            return diskStack.Pop();
            // ↑ 弹出栈顶元素，最上方的圆盘被移除
        }
        return null;
    }

    /// <summary>
    /// 查看柱子顶部圆盘（不取出）
    /// </summary>
    /// <returns>顶部圆盘，空柱返回 null</returns>
    public chh_Disk PeekTop()
    {
        if (diskStack.Count > 0)
        {
            return diskStack.Peek();
            // ↑ 只看不取，用于判断移动合法性
        }
        return null;
    }

    /// <summary>
    /// 获取此柱子上圆盘的数量
    /// </summary>
    public int GetDiskCount()
    {
        return diskStack.Count;
    }

    /// <summary>
    /// 清空此柱子上所有圆盘（重置游戏时使用）
    /// </summary>
    public void ClearDisks()
    {
        diskStack.Clear();
    }

    // ==================== 位置计算 ====================

    /// <summary>
    /// 计算新圆盘落在此柱子上时的最终世界坐标
    /// 根据当前已有的圆盘数量自动推算 Y 轴高度
    /// </summary>
    /// <returns>新圆盘应放置的世界坐标</returns>
    public Vector3 GetNextDiskPosition()
    {
        // X 和 Z 取柱子自身的位置
        float x = transform.position.x;
        float z = transform.position.z;

        // Y 轴计算：底座顶面 + 已有圆盘占用的高度 + 新圆盘半高
        // 例如：0 个盘时 Y = baseTopY + 0 * diskHeight = -0.325
        //       1 个盘时 Y = baseTopY + 1 * diskHeight = -0.175
        //       2 个盘时 Y = baseTopY + 2 * diskHeight = -0.025
        float y = baseTopY + diskStack.Count * diskHeight;

        return new Vector3(x, y, z);
    }
}
