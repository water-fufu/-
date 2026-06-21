using UnityEngine;
using System.Collections;

/// <summary>
/// 汉诺塔圆盘组件 — 挂载到每个圆盘预制体上（chh_Disk_1 / chh_Disk_2 / chh_Disk_3）
/// 职责：响应鼠标点击、高亮选中状态、执行三段式移动动画
/// </summary>
public class chh_Disk : MonoBehaviour
{
    // ==================== 公共属性（在 Inspector 中设置） ====================

    [Header("圆盘属性")]
    [Tooltip("大小等级：1=最大盘（蓝色），2=中盘（绿色），3=最小盘（红色）")]
    public int sizeLevel = 1;
    // ↑ sizeLevel 决定圆盘物理大小和移动合法性：
    //   数值越小 = 盘子越大，大盘不能放在小盘上面

    [Tooltip("选中高亮颜色（默认金黄色）")]
    public Color highlightColor = Color.yellow;

    // ==================== 私有变量 ====================

    private Color originalColor;
    // ↑ 记录圆盘原本的颜色，取消选中时恢复

    private Renderer diskRenderer;
    // ↑ 圆盘的 MeshRenderer 组件，用于动态修改材质颜色

    private chh_GameManager gameManager;
    // ↑ 场景中唯一的游戏管理器引用，所有点击事件转发给它处理

    private chh_Pillar currentPillar;
    // ↑ 圆盘当前所在的柱子引用，移动后需要更新

    private bool isSelected = false;
    // ↑ 标记当前是否处于选中状态（true=高亮中）

    // ==================== Unity 生命周期 ====================

    void Start()
    {
        // 获取自身 Renderer 组件
        diskRenderer = GetComponent<Renderer>();
        if (diskRenderer != null)
        {
            originalColor = diskRenderer.material.color;
            // 保存原始颜色，Deselect 时需要恢复
        }

        // 通过类型查找场景中唯一的 GameManager
        gameManager = FindObjectOfType<chh_GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("【chh_Disk】找不到 chh_GameManager！" +
                "请在 Hierarchy 中创建空物体并挂载 chh_GameManager.cs。");
        }
    }

    // ==================== 鼠标交互 ====================

    /// <summary>
    /// 鼠标点击此圆盘时，Unity 自动调用（需要对象有 Collider）
    /// 将事件转发给 GameManager 统一处理
    /// </summary>
    void OnMouseDown()
    {
        if (gameManager != null)
        {
            gameManager.OnDiskClicked(this);
            // ↑ 把"自己被点击了"这条消息告诉 GameManager
        }
    }

    // ==================== 选中 / 取消选中（由 GameManager 调用） ====================

    /// <summary>
    /// 选中此圆盘：将材质颜色改为高亮色，表示"当前玩家选中的圆盘"
    /// </summary>
    public void Select()
    {
        if (isSelected) return;  // 已选中，不用重复操作
        isSelected = true;
        diskRenderer.material.color = highlightColor;
    }

    /// <summary>
    /// 取消选中：恢复圆盘原本的颜色
    /// </summary>
    public void Deselect()
    {
        if (!isSelected) return;  // 未选中，不用操作
        isSelected = false;
        diskRenderer.material.color = originalColor;
    }

    // ==================== 柱子关联 ====================

    /// <summary>
    /// 设置圆盘当前所在的柱子（移动后由 GameManager 调用）
    /// </summary>
    /// <param name="pillar">目标柱子</param>
    public void SetPillar(chh_Pillar pillar)
    {
        currentPillar = pillar;
    }

    /// <summary>
    /// 获取圆盘当前所在柱子
    /// </summary>
    public chh_Pillar GetPillar()
    {
        return currentPillar;
    }

    // ==================== 动画：三段式移动 ====================

    /// <summary>
    /// 执行三段平滑动画：①垂直抬升 → ②水平平移 → ③垂直下降
    /// 动画完成后自动回调 GameManager.OnAnimationComplete()
    /// </summary>
    /// <param name="targetPosition">目标柱子上方的最终世界坐标</param>
    public IEnumerator AnimateMove(Vector3 targetPosition)
    {
        // -------- 动画参数（可调整）--------
        float liftHeight = 2.0f;
        // ↑ 抬升的高度（Y 轴），数值越大飞得越高
        float liftDuration = 0.25f;
        // ↑ 抬升阶段持续时间（秒）
        float moveDuration = 0.35f;
        // ↑ 平移阶段持续时间（秒）
        float dropDuration = 0.25f;
        // ↑ 下降阶段持续时间（秒）

        // -------- 计算三个关键位置 --------
        Vector3 startPos = transform.position;
        // ↑ 动画开始前圆盘所在位置

        Vector3 liftedPos = new Vector3(startPos.x, liftHeight, startPos.z);
        // ↑ 抬升后的位置：X、Z 不变，Y 抬到 liftHeight

        Vector3 aboveTarget = new Vector3(targetPosition.x, liftHeight, targetPosition.z);
        // ↑ 水平移动到目标上方（X、Z 对齐目标，Y 保持在抬升高度）

        // -------- 第一阶段：垂直抬升 --------
        float elapsed = 0f;
        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;                         // 累计经过的时间
            float t = elapsed / liftDuration;                 // 进度 [0, 1]
            transform.position = Vector3.Lerp(startPos, liftedPos, t);
            // ↑ Lerp 是线性插值：t=0 时在起点，t=1 时在终点
            yield return null;                                 // 暂停，等待下一帧
        }
        transform.position = liftedPos;  // 确保精确到位

        // -------- 第二阶段：水平平移 --------
        elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            transform.position = Vector3.Lerp(liftedPos, aboveTarget, t);
            yield return null;
        }
        transform.position = aboveTarget;

        // -------- 第三阶段：垂直下降 --------
        elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dropDuration;
            transform.position = Vector3.Lerp(aboveTarget, targetPosition, t);
            yield return null;
        }
        transform.position = targetPosition;  // 精确落位

        // -------- 动画结束，通知 GameManager --------
        gameManager.OnAnimationComplete();
        // ↑ 告诉 GameManager："我已经移动到目标了，请更新游戏状态"
    }
}
