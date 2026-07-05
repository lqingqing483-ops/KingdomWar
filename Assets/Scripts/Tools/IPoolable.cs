using UnityEngine;

namespace KingdomWar.Tools
{
    public interface IPoolable
    {
        void OnGet();
        void OnRelease();
    }
}
