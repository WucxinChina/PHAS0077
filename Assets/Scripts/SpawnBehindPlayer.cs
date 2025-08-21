using UnityEngine;
using Gley.TrafficSystem;

public class SpawnBehindPlayer : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform player;
    public KeyCode hotkey = KeyCode.H;

    [Tooltip("刷车相对玩家的距离（米），会投到最近的路点上")]
    public float spawnOffsetMeters = 60f;

    [Tooltip("相对玩家的方向：正数=在玩家前方；负数=在玩家后方")]
    public float forwardSign = -1f; // 默认在玩家后方

    [Tooltip("左右随机偏移，避免正好卡在一条线")]
    public float lateralJitterMeters = 6f;

    [Tooltip("要刷的车辆类型（需在 VehiclePool 里存在）")]
    public VehicleTypes vehicleType = VehicleTypes.Car;

    [Header("可选：立即给它一个目的地")]
    public bool setImmediateDestination = false;
    [Tooltip("目的地相对玩家前方的距离")]
    public float destinationAheadMeters = 200f;

    private void Reset()
    {
        // 若同物体上已有 TrafficComponent，就自动拿它的 player
        var tc = GetComponent<TrafficComponent>();
        if (tc != null) player = tc.player;
    }

    void Update()
    {
        if (player == null) return;

        if (Input.GetKeyDown(hotkey))
        {
            // 计算一个相对玩家的刷车点：前/后 + 轻微横向抖动，尽量不在可视范围
            Vector3 forward = player.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            float lateral = Random.Range(-lateralJitterMeters, lateralJitterMeters);
            Vector3 spawnPos = player.position
                              + forward * (forwardSign * Mathf.Abs(spawnOffsetMeters))
                              + right * lateral;

            if (setImmediateDestination)
            {
                // 给它一个大致的行驶方向（玩家前方某处），API 会取最近路点
                Vector3 dest = player.position + player.forward * destinationAheadMeters;

                API.InstantiateVehicleWithPath(
                    spawnPos,
                    vehicleType,
                    dest,
                    (vehicle, wpIndex) =>
                    {
                        Debug.Log($"[SpawnWithPath] {vehicle.name} at wp {wpIndex}");
                    }
                );
            }
            else
            {
                API.InstantiateVehicle(
                    spawnPos,
                    vehicleType,
                    (vehicle, wpIndex) =>
                    {
                        Debug.Log($"[Spawn] {vehicle.name} at wp {wpIndex}");
                    }
                );
            }
        }
    }
}
