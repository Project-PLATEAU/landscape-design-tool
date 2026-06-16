using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// OBB（回転付き境界ボックス）
/// center/rotation/extents は「Mesh のローカル空間」基準で返ります。
/// </summary>
public struct OrientedBounds
{
    public Vector3 center;      // 中心（Meshローカル）
    public Vector3 extents;     // 半辺長
    public Quaternion rotation; // ローカル空間内の回転（OBB基底）

    public readonly Vector3 Size => extents * 2f;

    public readonly Vector3 Forward => rotation * Vector3.forward; // OBBのZ軸
    public readonly Vector3 Up => rotation * Vector3.up;      // OBBのY軸
    public readonly Vector3 Right => rotation * Vector3.right;   // OBBのX軸

    public Vector3[] GetCorners()
    {
        var dirs = new Vector3[]
        {
            new Vector3(-1,-1,-1), new Vector3( 1,-1,-1),
            new Vector3(-1, 1,-1), new Vector3( 1, 1,-1),
            new Vector3(-1,-1, 1), new Vector3( 1,-1, 1),
            new Vector3(-1, 1, 1), new Vector3( 1, 1, 1),
        };
        var corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 localCorner = Vector3.Scale(dirs[i], extents);
            corners[i] = center + rotation * localCorner;
        }
        return corners;
    }
}

/// <summary>
/// OBB計算ユーティリティ
/// - PCA 版（Transform 非依存、ロール安定化）
/// - 法線指定版（Forward を明示）
/// </summary>
public static class BoundsUtil
{
    public static OrientedBounds GetLocalOBBFromMesh(
    Mesh mesh,
    Vector3? preferredUp = null,
    int jacobiIterations = 16)
    {
        var verts = mesh.vertices;
        if (verts == null || verts.Length == 0)
            return default;
        var bounds = mesh.bounds;

        return GetLocalOBBFromMesh(verts, bounds);
    }

    //======================================================================
    // 1) PCA ベース：GetLocalOBBFromMesh(mesh, preferredUp, jacobiIterations)
    //======================================================================
    /// <summary>
    /// Mesh の頂点分布から PCA によりローカル OBB を構築（Transform は使わない）
    /// ・preferredUp: ロール安定化に使う基準Up。未指定なら Vector3.up
    /// ・jacobiIterations: 固有分解の反復回数（8〜24程度で十分）
    /// </summary>
    public static OrientedBounds GetLocalOBBFromMesh(
        Vector3[] verts,
        Bounds bounds,
        Vector3? preferredUp = null,
        int jacobiIterations = 16)
    {
        if (verts == null || verts.Length == 0)
            return default;

        // 退化ケース: 頂点が極端に少ない場合は AABB へフォールバック
        if (verts.Length < 3)
        {
            var b = bounds;
            return new OrientedBounds { center = b.center, extents = b.extents, rotation = Quaternion.identity };
        }

        // 1) 重心
        Vector3 mean = Vector3.zero;
        for (int i = 0; i < verts.Length; i++) mean += verts[i];
        mean /= Mathf.Max(1, verts.Length);

        // 2) 共分散（対称3x3）
        double cxx = 0, cxy = 0, cxz = 0, cyy = 0, cyz = 0, czz = 0;
        for (int i = 0; i < verts.Length; i++)
        {
            var d = verts[i] - mean;
            double x = d.x, y = d.y, z = d.z;
            cxx += x * x; cxy += x * y; cxz += x * z;
            cyy += y * y; cyz += y * z; czz += z * z;
        }
        double invN = 1.0 / Math.Max(1, verts.Length);
        cxx *= invN; cxy *= invN; cxz *= invN;
        cyy *= invN; cyz *= invN; czz *= invN;

        Matrix3x3 C = new Matrix3x3(
            cxx, cxy, cxz,
            cxy, cyy, cyz,
            cxz, cyz, czz
        );

        // 3) 固有分解（V: 列が固有ベクトル, eval: 固有値）
        Matrix3x3 V = Matrix3x3.Identity;
        Vector3 eval = Vector3.zero;
        JacobiEigenDecomposition(ref C, ref V, ref eval, jacobiIterations);

        // 4) 固有値降順に軸を並べ替え（長い->短い）
        Vector3 ax0 = V.GetColumn(0).normalized;
        Vector3 ax1 = V.GetColumn(1).normalized;
        Vector3 ax2 = V.GetColumn(2).normalized;
        ReorderAxesByEigenvaluesDescending(ref ax0, ref ax1, ref ax2, ref eval);

        // 5) 右手系+ロール安定化（決定論的に符号を固定）
        Quaternion rot = BuildStableRotation(ax0, ax1, ax2, preferredUp ?? Vector3.up);

        // rot の列ベクトルが OBB の Right/Up/Forward
        Vector3 ex = rot * Vector3.right;
        Vector3 ey = rot * Vector3.up;
        Vector3 ez = rot * Vector3.forward;

        // 6) 投影レンジから center / extents を計算
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 d = verts[i] - mean;
            float sx = Vector3.Dot(d, ex);
            float sy = Vector3.Dot(d, ey);
            float sz = Vector3.Dot(d, ez);
            if (sx < minX) minX = sx; if (sx > maxX) maxX = sx;
            if (sy < minY) minY = sy; if (sy > maxY) maxY = sy;
            if (sz < minZ) minZ = sz; if (sz > maxZ) maxZ = sz;
        }

        Vector3 half = new Vector3(
            0.5f * (maxX - minX),
            0.5f * (maxY - minY),
            0.5f * (maxZ - minZ)
        );

        Vector3 localCenter =
              mean
            + ex * (0.5f * (minX + maxX))
            + ey * (0.5f * (minY + maxY))
            + ez * (0.5f * (minZ + maxZ));

        return new OrientedBounds
        {
            center = localCenter,
            extents = half,
            rotation = rot
        };
    }

    //======================================================================
    // 2) 法線指定版：GetLocalOBBFromMeshWithNormal(mesh, normal, referenceUp, refineInPlane)
    //======================================================================
    /// <summary>
    /// 既知の法線（=Forward）を使って OBB を構築。
    /// ・normal: OBB の Forward(=Z) にしたい方向（符号は反転しません）
    /// ・referenceUp: ロール固定に使う基準Up（未指定時は Vector3.up）
    /// ・refineInPlane: true で平面内 2D-PCA により Right/Up を形状に最適化（Forward は固定）
    /// </summary>
    public static OrientedBounds GetLocalOBBFromMeshWithNormal(
        Mesh mesh,
        Vector3 normal,
        Vector3? referenceUp = null,
        bool refineInPlane = true)
    {
        var verts = mesh.vertices;
        if (verts == null || verts.Length == 0)
            return default;

        if (verts.Length < 3)
        {
            var b = mesh.bounds;
            return new OrientedBounds { center = b.center, extents = b.extents, rotation = Quaternion.identity };
        }

        // 重心
        Vector3 mean = Vector3.zero;
        for (int i = 0; i < verts.Length; i++) mean += verts[i];
        mean /= Mathf.Max(1, verts.Length);

        // Forward = normal、Up は referenceUp を直交投影して作る（ロール固定）
        Vector3 f = normal.normalized;
        Vector3 upRef = referenceUp ?? Vector3.up;
        Vector3 upProj = upRef - Vector3.Dot(upRef, f) * f;
        if (upProj.sqrMagnitude < 1e-10f)
        {
            // ほぼ平行だったとき：f に最も直交に近い世界軸を選んで投影
            Vector3[] picks = { Vector3.right, Vector3.up, Vector3.forward };
            float best = -1f; int bestIdx = 0;
            for (int i = 0; i < picks.Length; i++)
            {
                float s = 1f - Mathf.Abs(Vector3.Dot(picks[i], f));
                if (s > best) { best = s; bestIdx = i; }
            }
            upProj = Vector3.ProjectOnPlane(picks[bestIdx], f);
        }
        Vector3 u = upProj.normalized;               // Up
        Vector3 r = Vector3.Cross(u, f).normalized;  // Right
        u = Vector3.Cross(f, r).normalized;          // 再直交化

        // 任意: 平面内 2D-PCA（Forward は固定）
        if (refineInPlane)
        {
            double a = 0, b = 0, c = 0; // 2x2共分散 [[a,b],[b,c]]
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 d = verts[i] - mean;
                double sx = Vector3.Dot(d, r);
                double sy = Vector3.Dot(d, u);
                a += sx * sx; b += sx * sy; c += sy * sy;
            }
            double invN = 1.0 / Mathf.Max(1, verts.Length);
            a *= invN; b *= invN; c *= invN;

            // 最大固有ベクトルの角度（対称2x2の閉形式）
            double theta = 0.5 * System.Math.Atan2(2.0 * b, (a - c));
            float cs = (float)System.Math.Cos(theta);
            float sn = (float)System.Math.Sin(theta);

            Vector3 r2 = (cs * r + sn * u).normalized;
            Vector3 u2 = Vector3.Cross(f, r2).normalized; // 右手系維持
            // 符号を固定（不要な180°フリップ回避。元のrに近い向きを選ぶ）
            if (Vector3.Dot(r2, r) < 0f) { r2 = -r2; u2 = -u2; }
            r = r2; u = u2;
        }

        Quaternion rot = QuaternionFromAxes(r, u, f);

        // 射影レンジ
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 d = verts[i] - mean;
            float sx = Vector3.Dot(d, r);
            float sy = Vector3.Dot(d, u);
            float sz = Vector3.Dot(d, f);
            if (sx < minX) minX = sx; if (sx > maxX) maxX = sx;
            if (sy < minY) minY = sy; if (sy > maxY) maxY = sy;
            if (sz < minZ) minZ = sz; if (sz > maxZ) maxZ = sz;
        }

        Vector3 half = new Vector3(
            0.5f * (maxX - minX),
            0.5f * (maxY - minY),
            0.5f * (maxZ - minZ)
        );

        Vector3 localCenter =
              mean
            + r * (0.5f * (minX + maxX))
            + u * (0.5f * (minY + maxY))
            + f * (0.5f * (minZ + maxZ));

        return new OrientedBounds
        {
            center = localCenter,
            extents = half,
            rotation = rot
        };
    }

    // 追記：形を保ったまま Forward の向きだけ合わせる
    public static OrientedBounds AlignForwardToNormal(
        OrientedBounds obbLocal,
        Vector3 forwardHint,          // ローカル法線（※ワールドなら InverseTransformDirection してから渡す）
        Vector3? upForFlipAxis = null // 反転軸に使う Up（nullなら obbLocal.Up）
    )
    {
        Vector3 f = obbLocal.Forward;
        Vector3 target = forwardHint.normalized;
        if (Vector3.Dot(f, target) >= 0f) return obbLocal; // もう同じ向き

        // 右手系維持のため Up 軸回りで180°回転（Forward/Rightの符号だけ反転）
        Vector3 upAxis = upForFlipAxis ?? obbLocal.Up;
        Quaternion flip = Quaternion.AngleAxis(180f, upAxis);
        obbLocal.rotation = flip * obbLocal.rotation;
        // center/extents は一切変更しない
        return obbLocal;
    }

    /// <summary>
    /// Mesh から OrientedBounds を作る
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="triangleIndex"></param>
    /// <returns></returns>
    public static OrientedBounds GetOrientedBoundsFromMesh(Mesh mesh, List<int[]> triangleIndex)
    {
        if (mesh == null) return default;

        var verts = mesh.vertices;
        if (verts == null || verts.Length == 0) return default;
        var bounds = mesh.bounds;
        return GetOrientedBoundsFromMesh(verts, bounds, triangleIndex);
    }

    public static OrientedBounds GetOrientedBoundsFromMesh(Vector3[] verts, Bounds bounds, List<int[]> triangleIndex)
    {
        // 参照三角形が未指定なら既存のPCA版にフォールバック
        if (triangleIndex == null || triangleIndex.Count == 0)
            return GetLocalOBBFromMesh(verts, bounds);

        // 三角形リストから使う頂点を収集（重複は1回だけ数える）
        var used = new HashSet<int>();
        var pts = new List<Vector3>();
        foreach (var tri in triangleIndex)
        {
            if (tri == null) continue;
            for (int k = 0; k < tri.Length; k++)
            {
                int idx = tri[k];
                if (idx < 0 || idx >= verts.Length) continue; // 範囲外は無視
                if (used.Add(idx)) pts.Add(verts[idx]);
            }
        }

        // 有効な頂点が無ければフォールバック
        if (pts.Count == 0) return GetLocalOBBFromMesh(verts, bounds);

        // 頂点が少なすぎる場合は「その集合のAABB」を返す（回転は無し）
        if (pts.Count < 3)
        {
            Vector3 minV = pts[0], maxV = pts[0];
            for (int i = 1; i < pts.Count; i++)
            {
                minV = Vector3.Min(minV, pts[i]);
                maxV = Vector3.Max(maxV, pts[i]);
            }
            return new OrientedBounds
            {
                center = 0.5f * (minV + maxV),
                extents = 0.5f * (maxV - minV),
                rotation = Quaternion.identity
            };
        }

        // 1) 重心
        Vector3 mean = Vector3.zero;
        for (int i = 0; i < pts.Count; i++) mean += pts[i];
        mean /= Mathf.Max(1, pts.Count);

        // 2) 共分散（対称3x3）
        double cxx = 0, cxy = 0, cxz = 0, cyy = 0, cyz = 0, czz = 0;
        for (int i = 0; i < pts.Count; i++)
        {
            var d = pts[i] - mean;
            double x = d.x, y = d.y, z = d.z;
            cxx += x * x; cxy += x * y; cxz += x * z;
            cyy += y * y; cyz += y * z; czz += z * z;
        }
        double invN = 1.0 / System.Math.Max(1, pts.Count);
        cxx *= invN; cxy *= invN; cxz *= invN;
        cyy *= invN; cyz *= invN; czz *= invN;

        Matrix3x3 C = new Matrix3x3(
            cxx, cxy, cxz,
            cxy, cyy, cyz,
            cxz, cyz, czz
        );

        // 3) 固有分解（V: 列が固有ベクトル, eval: 固有値）
        Matrix3x3 V = Matrix3x3.Identity;
        Vector3 eval = Vector3.zero;
        JacobiEigenDecomposition(ref C, ref V, ref eval, 16);

        // 4) 固有値降順に軸を並べ替え（長い->短い）
        Vector3 ax0 = V.GetColumn(0).normalized;
        Vector3 ax1 = V.GetColumn(1).normalized;
        Vector3 ax2 = V.GetColumn(2).normalized;
        ReorderAxesByEigenvaluesDescending(ref ax0, ref ax1, ref ax2, ref eval);

        // 5) 右手系+ロール安定化（preferredUp=Vector3.up）
        Quaternion rot = BuildStableRotation(ax0, ax1, ax2, Vector3.up);

        // 基底ベクトル
        Vector3 ex = rot * Vector3.right;
        Vector3 ey = rot * Vector3.up;
        Vector3 ez = rot * Vector3.forward;

        // 6) 投影レンジから center / extents を計算
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;

        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 d = pts[i] - mean;
            float sx = Vector3.Dot(d, ex);
            float sy = Vector3.Dot(d, ey);
            float sz = Vector3.Dot(d, ez);
            if (sx < minX) minX = sx; if (sx > maxX) maxX = sx;
            if (sy < minY) minY = sy; if (sy > maxY) maxY = sy;
            if (sz < minZ) minZ = sz; if (sz > maxZ) maxZ = sz;
        }

        Vector3 half = new Vector3(
            0.5f * (maxX - minX),
            0.5f * (maxY - minY),
            0.5f * (maxZ - minZ)
        );

        Vector3 localCenter =
              mean
            + ex * (0.5f * (minX + maxX))
            + ey * (0.5f * (minY + maxY))
            + ez * (0.5f * (minZ + maxZ));

        return new OrientedBounds
        {
            center = localCenter,
            extents = half,
            rotation = rot
        };
    }


    //======================================================================
    // 3) ローカル → ワールド変換
    //======================================================================
    /// <summary>ローカル OBB をワールドへ（非等方スケール対応）。</summary>
    public static OrientedBounds ToWorldOBB(OrientedBounds localObb, Transform t)
    {
        Vector3 scaledExtents = new Vector3(
            Mathf.Abs(t.lossyScale.x) * localObb.extents.x,
            Mathf.Abs(t.lossyScale.y) * localObb.extents.y,
            Mathf.Abs(t.lossyScale.z) * localObb.extents.z
        );

        return new OrientedBounds
        {
            center = t.TransformPoint(localObb.center),
            rotation = t.rotation * localObb.rotation,
            extents = scaledExtents
        };
    }

    //================ ユーティリティ（安定化/固有分解など） ================//

    // 列ベクトル＝基底軸（Right/Up/Forward）として正しくクォータニオン化
    private static Quaternion QuaternionFromAxes(Vector3 xAxis, Vector3 yAxis, Vector3 zAxis)
    {
        Matrix4x4 m = new Matrix4x4();
        m.SetColumn(0, new Vector4(xAxis.x, xAxis.y, xAxis.z, 0));
        m.SetColumn(1, new Vector4(yAxis.x, yAxis.y, yAxis.z, 0));
        m.SetColumn(2, new Vector4(zAxis.x, zAxis.y, zAxis.z, 0));
        m.SetColumn(3, new Vector4(0, 0, 0, 1));
        return m.rotation;
    }

    // 右手系の再構築＋ロール固定（preferredUp を forward に直交投影）
    private static Quaternion BuildStableRotation(Vector3 ax0, Vector3 ax1, Vector3 ax2, Vector3 preferredUp)
    {
        // 正規化
        ax0 = ax0.normalized;
        ax1 = ax1.normalized;
        ax2 = ax2.normalized;

        // forward（Z）を ax2 として採用
        Vector3 f = ax2;

        // ロール安定化：preferredUp を f に直交投影し Up を決める
        Vector3 upProj = preferredUp - Vector3.Dot(preferredUp, f) * f;
        if (upProj.sqrMagnitude < 1e-10f) upProj = ax1; // ほぼ平行なら第2主成分へフォールバック
        Vector3 u = upProj.normalized;

        // Right を直交に構築、Up を再直交化
        Vector3 r = Vector3.Cross(u, f).normalized;
        u = Vector3.Cross(f, r).normalized;

        // 符号規約：できるだけ +X,+Y を指す（決定論的に符号固定）
        if (Vector3.Dot(r, Vector3.right) < 0f) { r = -r; u = -u; } // fは据え置き
        if (Vector3.Dot(u, Vector3.up) < 0f) { u = -u; f = -f; } // rは据え置き

        return QuaternionFromAxes(r, u, f);
    }

    private static void ReorderAxesByEigenvaluesDescending(ref Vector3 ax0, ref Vector3 ax1, ref Vector3 ax2, ref Vector3 eval)
    {
        (float v, Vector3 a)[] arr = new (float, Vector3)[]
        {
            (eval.x, ax0), (eval.y, ax1), (eval.z, ax2)
        };
        System.Array.Sort(arr, (p, q) => q.v.CompareTo(p.v)); // 降順ソート
        ax0 = arr[0].a; ax1 = arr[1].a; ax2 = arr[2].a;
        eval = new Vector3(arr[0].v, arr[1].v, arr[2].v);
    }

    // ---- 3x3 対称行列の Jacobi 固有分解 ----
    private struct Matrix3x3
    {
        // 行優先: [r0c0 r0c1 r0c2; r1c0 r1c1 r1c2; r2c0 r2c1 r2c2]
        public double m00, m01, m02;
        public double m10, m11, m12;
        public double m20, m21, m22;

        public Matrix3x3(double m00, double m01, double m02,
                         double m10, double m11, double m12,
                         double m20, double m21, double m22)
        {
            this.m00 = m00; this.m01 = m01; this.m02 = m02;
            this.m10 = m10; this.m11 = m11; this.m12 = m12;
            this.m20 = m20; this.m21 = m21; this.m22 = m22;
        }

        public static Matrix3x3 Identity => new Matrix3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);

        public Vector3 GetColumn(int i)
        {
            switch (i)
            {
                case 0: return new Vector3((float)m00, (float)m10, (float)m20);
                case 1: return new Vector3((float)m01, (float)m11, (float)m21);
                default: return new Vector3((float)m02, (float)m12, (float)m22);
            }
        }
    }

    private static void JacobiEigenDecomposition(ref Matrix3x3 A, ref Matrix3x3 V, ref Vector3 eval, int iterations)
    {
        V = Matrix3x3.Identity;

        for (int it = 0; it < iterations; it++)
        {
            double a01 = System.Math.Abs(A.m01);
            double a02 = System.Math.Abs(A.m02);
            double a12 = System.Math.Abs(A.m12);

            int p = 0, q = 1;
            double max = a01;
            if (a02 > max) { max = a02; p = 0; q = 2; }
            if (a12 > max) { max = a12; p = 1; q = 2; }

            if (max < 1e-15) break; // 収束

            double app = Get(A, p, p);
            double aqq = Get(A, q, q);
            double apq = Get(A, p, q);
            double phi = 0.5 * System.Math.Atan2(2.0 * apq, (aqq - app));
            double c = System.Math.Cos(phi);
            double s = System.Math.Sin(phi);

            ApplyJacobi(ref A, p, q, c, s);    // A <- G^T A G
            ApplyJacobiToV(ref V, p, q, c, s); // V <- V G
        }

        eval = new Vector3((float)A.m00, (float)A.m11, (float)A.m22); // 対角が固有値
        // V の列が固有ベクトル
    }

    private static double Get(in Matrix3x3 A, int r, int c)
    {
        if (r == 0 && c == 0) return A.m00;
        if (r == 0 && c == 1) return A.m01;
        if (r == 0 && c == 2) return A.m02;
        if (r == 1 && c == 0) return A.m10;
        if (r == 1 && c == 1) return A.m11;
        if (r == 1 && c == 2) return A.m12;
        if (r == 2 && c == 0) return A.m20;
        if (r == 2 && c == 1) return A.m21;
        return A.m22;
    }

    private static void Set(ref Matrix3x3 A, int r, int c, double v)
    {
        if (r == 0 && c == 0) A.m00 = v;
        else if (r == 0 && c == 1) A.m01 = v;
        else if (r == 0 && c == 2) A.m02 = v;
        else if (r == 1 && c == 0) A.m10 = v;
        else if (r == 1 && c == 1) A.m11 = v;
        else if (r == 1 && c == 2) A.m12 = v;
        else if (r == 2 && c == 0) A.m20 = v;
        else if (r == 2 && c == 1) A.m21 = v;
        else A.m22 = v;
    }

    private static void ApplyJacobi(ref Matrix3x3 A, int p, int q, double c, double s)
    {
        // 行・列を同時回転（対称性維持）
        for (int k = 0; k < 3; k++)
        {
            double Apk = Get(A, p, k);
            double Aqk = Get(A, q, k);
            Set(ref A, p, k, c * Apk - s * Aqk);
            Set(ref A, q, k, s * Apk + c * Aqk);
        }
        for (int k = 0; k < 3; k++)
        {
            double Akp = Get(A, k, p);
            double Akq = Get(A, k, q);
            Set(ref A, k, p, c * Akp - s * Akq);
            Set(ref A, k, q, s * Akp + c * Akq);
        }
    }

    private static void ApplyJacobiToV(ref Matrix3x3 V, int p, int q, double c, double s)
    {
        for (int k = 0; k < 3; k++)
        {
            double Vkp = Get(V, k, p);
            double Vkq = Get(V, k, q);
            Set(ref V, k, p, c * Vkp - s * Vkq);
            Set(ref V, k, q, s * Vkp + c * Vkq);
        }
    }
}
