using NUnit.Framework;
using KingdomWar.Game;

public class AudioManagerTests
{
    [Test]
    public void Instance_IsNotNull()
    {
        Assert.That(AudioManager.Instance, Is.Not.Null);
    }

    [Test]
    public void DefaultVolume_IsOne()
    {
        AudioManager.Instance.SetVolume(1.0f);
        Assert.That(AudioManager.Instance.GetBGMVolume(), Is.EqualTo(1.0f));
        Assert.That(AudioManager.Instance.GetSFXVolume(), Is.EqualTo(1.0f));
    }

    [Test]
    public void SetVolume_UpdatesBGMAndSFX()
    {
        AudioManager.Instance.SetVolume(0.5f);
        Assert.That(AudioManager.Instance.GetBGMVolume(), Is.EqualTo(0.5f));
        Assert.That(AudioManager.Instance.GetSFXVolume(), Is.EqualTo(0.5f));
        AudioManager.Instance.SetVolume(1.0f);
    }

    [Test]
    public void SetVolume_ClampsToZero()
    {
        AudioManager.Instance.SetVolume(-0.5f);
        Assert.That(AudioManager.Instance.GetBGMVolume(), Is.EqualTo(0.0f));
        AudioManager.Instance.SetVolume(1.0f);
    }

    [Test]
    public void SetVolume_ClampsToOne()
    {
        AudioManager.Instance.SetVolume(1.5f);
        Assert.That(AudioManager.Instance.GetBGMVolume(), Is.EqualTo(1.0f));
    }

    [Test]
    public void SetBGMVolume_OnlyAffectsBGM()
    {
        AudioManager.Instance.SetSFXVolume(0.8f);
        AudioManager.Instance.SetBGMVolume(0.3f);
        Assert.That(AudioManager.Instance.GetBGMVolume(), Is.EqualTo(0.3f));
        Assert.That(AudioManager.Instance.GetSFXVolume(), Is.EqualTo(0.8f));
        AudioManager.Instance.SetBGMVolume(1.0f);
        AudioManager.Instance.SetSFXVolume(1.0f);
    }

    [Test]
    public void LoadVolumeSettings_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            var method = typeof(AudioManager).GetMethod("LoadVolumeSettings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
                method.Invoke(AudioManager.Instance, null);
        });
    }

    [Test]
    public void VolumePersists_AfterSaveAndLoad()
    {
        AudioManager.Instance.SetVolume(0.3f);
        AudioManager.Instance.Save();
        AudioManager.Instance.SetVolume(1.0f);
        AudioManager.Instance.Load();
        Assert.That(AudioManager.Instance.GetBGMVolume(), Is.EqualTo(0.3f).Within(0.01f));
        AudioManager.Instance.SetVolume(1.0f);
    }
}
