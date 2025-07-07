using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Landscape2.Runtime.LandscapePlanLoader
{
    /// <summary>
    /// 景観計画区画の機能に対しての規制をまとめたクラス
    /// 例えば、景観計画区画に登録時に問題ないかチェックする際のルールは編集結果の反映時にも利用されるはず　
    /// 　という考えのルールを変数、関数で実装する
    /// </summary>
    public static class AreaPlanningModuleRegulation
    {
        // 必要なピンの数
        // AreaPlanningCollisionHandlerで convexを有効にしているため頂点となるピンが4以上必要
        public const int NumRequiredPins = 4;
    }
}
