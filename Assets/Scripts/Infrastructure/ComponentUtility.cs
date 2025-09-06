using UnityEngine;

namespace Core
{
    /// <summary>
    /// コンポーネント操作のユーティリティクラス
    /// </summary>
    public static class ComponentUtility
    {
        /// <summary>
        /// 指定されたGameObjectから指定されたコンポーネントを取得し、存在しない場合は追加する
        /// </summary>
        /// <typeparam name="T">取得または追加するコンポーネントの型</typeparam>
        /// <param name="gameObject">対象のGameObject</param>
        /// <returns>取得または追加されたコンポーネント</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent(out T component))
            {
                return component;
            }
            else
            {
                return gameObject.AddComponent<T>();
            }
        }

        /// <summary>
        /// 指定されたMonoBehaviourのGameObjectから指定されたコンポーネントを取得し、存在しない場合は追加する
        /// </summary>
        /// <typeparam name="T">取得または追加するコンポーネントの型</typeparam>
        /// <param name="monoBehaviour">対象のMonoBehaviour</param>
        /// <returns>取得または追加されたコンポーネント</returns>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour) where T : Component
        {
            return monoBehaviour.gameObject.GetOrAddComponent<T>();
        }
    }
}
