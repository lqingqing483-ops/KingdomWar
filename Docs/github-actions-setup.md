# GitHub Actions CI/CD 接入指南

## 前提条件

- GitHub 账号（免费）
- Unity 2022.3.57f1 许可证（个人版免费）

---

## Step 1: 在 GitHub 上创建仓库

1. 打开 https://github.com/new
2. 仓库名填 `KingdomWar`
3. **不要勾** "Add a README"、"Add .gitignore"、"Choose a license"
4. 点击 "Create repository"

## Step 2: 推送本地代码

```bash
# 在本地 KingdomWar 目录执行：
git remote add github https://github.com/你的用户名/KingdomWar.git
git push github master --tags
```

## Step 3: 激活 Unity 并导出许可证

GitHub Actions 的 Runner 没有安装 Unity，需要用 game-ci 提供的激活工具。

### 方法 A：使用 unity-activator（推荐）

```bash
# 安装激活工具
git clone https://github.com/nicedoc/unity-activator
cd unity-activator
dotnet run -- -u "你的Unity邮箱" -p "你的Unity密码" -o license.ulf
```

### 方法 B：手动导出（在本地 Unity 操作）

1. 本地打开 Unity Hub → 登录账号
2. 菜单栏：`Help → Manage License`
3. 点击 "Return License" 旁边的图标，选择 "Save License"
4. 保存为 `UnityLicense.ulf`

> ⚠️ **注意**：个人版许可证每个月的激活次数有限，建议用方法 A

## Step 4: 上传许可证到 GitHub Secrets

```bash
# 方法一：用 GitHub CLI
gh secret set UNITY_LICENSE < UnityLicense.ulf

# 方法二：手动操作
# GitHub 网页 → 仓库 Settings → Secrets and variables → Actions
# → New repository secret
# Name: UNITY_LICENSE
# Value: 粘贴 license.ulf 的全部内容
```

## Step 5: 触发一次 CI

推送任意修改：

```bash
git commit --allow-empty -m "test: trigger GitHub Actions"
git push github master
```

然后打开 GitHub 仓库 → Actions 标签页，可以看到流水线在跑。

## Step 6: 验证结果

流水线跑完大约需要 15-30 分钟，5 个 Job：

```
✅ test-editmode   → 140个 EditMode 测试
✅ test-playmode   → 60个 PlayMode 测试
✅ build-windows   → 生成 exe 安装包
⏭️ build-android   → 仅打 tag 时触发
```

## 常见问题

### Q: 第一次跑报 "No license found"
第一次激活 Unity 许可证需要手动确认。看 Actions 日志里的提示，按步骤操作即可。

### Q: 免费额度够不够？
GitHub Actions 每月 2000 分钟免费。一次完整跑约 25 分钟，2000 / 25 = **80 次/月**，个人开发完全够。

### Q: 需要保留 GitLab 吗？
可以双推：

```bash
# 同时推送到两个平台
git push github master
git push gitlab master
```
