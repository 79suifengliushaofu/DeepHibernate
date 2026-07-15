# DeepHibernate — 极致休眠

一键进入深度休眠，按需切断外设 IO 供电。极简原生 Windows 工具，零依赖，20KB 单文件。

![Platform](https://img.shields.io/badge/platform-Windows%207%2B-blue)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.x-green)
![Size](https://img.shields.io/badge/size-20%20KB-lightgrey)
![Version](https://img.shields.io/badge/version-2.0-blue)
![License](https://img.shields.io/badge/license-MIT-yellow)

---

## 兼容性

| 系统 | 状态 | 说明 |
|---|---|---|
| Windows 11 | ✅ 完全支持 | 开发与测试环境 |
| Windows 10 | ✅ 完全支持 | .NET 4.x 内置 |
| Windows 8 / 8.1 | ✅ 完全支持 | .NET 4.x 内置 |
| Windows 7 SP1+ | ✅ 完全支持 | 需安装 .NET Framework 4.0+（系统更新自带） |
| Windows Server 2012+ | ✅ 支持 | powercfg 功能一致 |

**不依赖任何第三方运行时**，使用系统原生 `powercfg` + `shutdown` 命令，无注册表残留，无后台进程。

---

## 功能

打开后四个选项，按需勾选：

| 选项 | 默认 | 作用 |
|---|---|---|
| 关闭 USB 选择性暂停 | ☑ | 休眠后切断 USB 外设持续供电 |
| 禁用设备唤醒权限 | ☑ | 键盘 / 鼠标等外围设备无法唤醒电脑 |
| 关闭网卡 Wake-on-LAN | ☐ | 勾选后远程网络唤醒失效（默认保留） |
| 禁用计划任务唤醒 | ☑ | 阻止定时任务唤醒系统 |

## 智能降温等待

点击「进入极致休眠」后，程序会**先等待 CPU 温度降至目标温度**，再执行休眠，避免高温直接断电损伤硬件。

- 可自定义目标温度：滑块 + 数字输入，20~80°C，默认 35°C
- 窗口实时显示 **CPU 温度**（WMI `MSAcpi_ThermalZoneTemperature`）和 **GPU 温度**（`nvidia-smi`）
- 降温达标后进入 S4 休眠
- 降温前自动切换高性能电源方案（主动散热）
- 最长等待 10 分钟，超时后直接休眠（防止无限等待）

点击「进入极致休眠」→ 主动散热电源策略 → 降温等待 → 温度达标 → 系统进入 S4 休眠。

---

## 使用方式

1. 下载 `DeepHibernate.exe`
2. 双击运行（建议右键 → **以管理员身份运行**，确保 powercfg 策略写入成功）
3. 勾选需要切断的 IO 功能
4. 拖动滑块设置目标温度（默认 35°C）
5. 点击按钮，等待休眠

> 休眠后 IO 彻底断电还依赖主板 BIOS 中的 **ErP / EuP** 设置。如需完全切断，请进 BIOS 开启 ErP Ready 并将 USB Power in S4/S5 设为 Disabled。

---

## 技术实现

- **GUI**：WinForms（.NET Framework 4.x），极简窗口，不驻任务栏，用完即释放
- **CPU 温度**：WMI 查询 `MSAcpi_ThermalZoneTemperature`（开尔文十分之一度 → 摄氏度）
- **GPU 温度**：`nvidia-smi --query-gpu=temperature.gpu`（不可用时显示 N/A）
- **降温等待**：以 2 秒间隔轮询温度，达标后执行休眠，最长超时 10 分钟
- **主动散热**：休眠前自动切换 `8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c` 高性能电源方案
- **电源策略**：通过 `powercfg /SETACVALUEINDEX` 修改 USB 选择性暂停
- **设备唤醒**：`powercfg -devicequery wake_armed` 枚举后逐个 `-devicedisablewake`
- **网卡 WOL**：WMIC 调用 `Win32_NetworkAdapter.EnableWakeUp`
- **计划唤醒**：`powercfg -waketimers disable`
- **休眠**：`shutdown /h /f`

---

## 更新日志

### v2.0 (2026-07-15)
- 重新编译，保留 v2.0 四个选项框 UI
- 目标温度改为可自定义：滑块 + 数字输入，20~80°C，默认 35°C
- 新增 GPU 温度实时显示（nvidia-smi）
- 新增主动散热电源策略（高性能方案）
- CPU 温度显示颜色指示（高于目标红色，达标绿色）

### v1.0 (2025-07-01)
- 四个选项框：USB 暂停 / 设备唤醒 / WOL / 计划任务
- 一键进入 S4 休眠

---

## 构建

```bash
csc /target:winexe /win32icon:DeepHibernate.ico /out:DeepHibernate.exe DeepHibernate.cs ^
    /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Management.dll
```

要求：Windows SDK 自带 csc.exe（`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`）。

---

## License

MIT
