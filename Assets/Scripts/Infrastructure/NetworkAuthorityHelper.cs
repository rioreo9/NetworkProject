using System;
using Fusion;

namespace Core.Utils
{
    /// <summary>
    /// Fusion2のネットワーク権限チェックとRPC処理を簡単にするユーティリティクラス
    /// </summary>
    public static class NetworkAuthorityHelper
    {
        /// <summary>
        /// 権限に応じて適切な処理を実行する
        /// StateAuthorityがある場合は直接実行、ない場合はRPCでリクエスト
        /// </summary>
        /// <param name="networkBehaviour">対象のNetworkBehaviour</param>
        /// <param name="directAction">StateAuthorityがある場合に実行するアクション</param>
        /// <param name="rpcAction">StateAuthorityがない場合に実行するRPCアクション</param>
        public static void ExecuteWithAuthority(NetworkBehaviour networkBehaviour, Action directAction, Action rpcAction)
        {
            if (networkBehaviour == null || networkBehaviour.Object == null)
            {
                // networkBehaviourまたはObjectがnullの場合は何もしない
                return;
            }

            if (networkBehaviour.Object.HasStateAuthority)
            {
                // サーバーまたは権限を持つクライアントは直接実行
                directAction?.Invoke();
            }
            else
            {
                // 権限がないクライアントはRPCでリクエスト
                rpcAction?.Invoke();
            }
        }

        /// <summary>
        /// 権限に応じて適切な処理を実行する（戻り値あり）
        /// </summary>
        /// <typeparam name="T">戻り値の型</typeparam>
        /// <param name="networkBehaviour">対象のNetworkBehaviour</param>
        /// <param name="directAction">StateAuthorityがある場合に実行するアクション</param>
        /// <param name="rpcAction">StateAuthorityがない場合に実行するRPCアクション</param>
        /// <param name="defaultValue">RPCの場合の戻り値（通常はdefault値）</param>
        /// <returns>directActionの戻り値またはdefaultValue</returns>
        public static T ExecuteWithAuthority<T>(NetworkBehaviour networkBehaviour, Func<T> directAction, Action rpcAction, T defaultValue = default)
        {
            if (networkBehaviour.Object.HasStateAuthority)
            {
                return directAction != null ? directAction.Invoke() : defaultValue;
            }
            else
            {
                rpcAction?.Invoke();
                return defaultValue;
            }
        }
    }
}
