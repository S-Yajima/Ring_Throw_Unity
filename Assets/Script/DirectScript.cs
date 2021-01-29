using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectScript : MonoBehaviour
{
    // 力を加える向きの単位ベクトル
    private Vector3 force_n_v3 = new Vector3(1.0f, 0.0f, 0.0f);
    // 加える力の角度。
    private int force_angle = 30;
    private float force_cos_val = 0.0f;
    private float force_sin_val = 0.0f;
    // UnityのPhysicsで設定されている重力加速度の値を取得する。
    private float G = Mathf.Abs(Physics.gravity.y);

    // リングのゲームオブジェクト。
    public GameObject ring_prefab_silver;
    public GameObject ring_prefab_red;
    public GameObject ring_prefab_green;
    private GameObject[] ring = new GameObject[3];
    // ポールのゲームオブジェクト
    private GameObject[] pole = new GameObject[4];
    // リングとポールの処理対象インデックス番号
    private int target_pole_index = 0;
    private int target_ring_index = 0;

    // 時間測定用の変数
    private float delta_time = 0.0f;
    private float interval_time = 7.0f;


    /*
    * リングからポールまでの距離を計算
    * 高さ(Y軸)は考慮せず、X-Z平面の距離のみを算出する
    */
    float distance_ring_to_pole_XZ(Vector3 ring_pos_v3, Vector3 pole_pos_v3)
    {
        Vector3 v = pole_pos_v3 - ring_pos_v3;
        v.y = 0.0f;             // Y軸は考慮しないため0に設定する。

        return v.magnitude;
    }


    /*
    * リングからポールまでのY軸(x-z)角度を計算
    * 戻り値 : int リングからポールまでのY軸(x-z平面)角度。degree値
    * (1) Scene の座標は Z軸手前がマイナス。
    * (2) GameObject.Transform の Rotation Y軸 は初期状態から手前への回転がプラス
    * 上記(1)(2)をフィックスするため、z座標の値をマイナスした値をAtan()で計算する
    */
    int angle_ring_to_pole_XZ(Vector3 ring_pos_v3, Vector3 pole_pos_v3)
    {
        Vector3 v = pole_pos_v3 - ring_pos_v3;

        return (int)(Mathf.Atan((-1 * v.z) / v.x) * Mathf.Rad2Deg);
    }


    /*
    * リングからポールまで投射する初速度を算出する
    * 戻り値 : float 初速度 m/s の値
    * 当メソッドの戻り値と単位ベクトルをかけ、投射初速度のベクトルを生成する
    * 
    * 初速度 v / 投射角度 θ / 重力加速度 G とすると下記が成り立つ
    * 式) v * cosθ * ((v * sinθ) / G) * 2 = 水平到達距離
    * 
    * 上記の式を元に水平到達距離から初速度を算出する式は
    * v * cosθ * ((v * sinθ) / G) = 水平到達距離 / 2
    * ↓
    * v * v * cosθ = (水平到達距離 / 2) * (G / sinθ)
    * ↓
    * v * v = (水平到達距離 / 2) * (G / sinθ) * (1 / cosθ)
    * ↓
    * 式) v = Sqrt(水平到達距離 * G / (2 * cos * sin))
    */
    float first_velocity(float distance)
    {
        float velocity = 0.0f;
        if(this.force_cos_val != 0 && this.force_sin_val != 0)
        {
            velocity = Mathf.Sqrt(distance * this.G / (2 * this.force_cos_val * this.force_sin_val));
        }

        return velocity;
    }


    /*
    * 一定時間毎に自動で実行される。
    * 物理演算は当メソッドで実行する。
    */
    void FixedUpdate()
    {
        // 1秒間ごとにリング投射。インデックスの分投げ切った後、4秒休止。
        this.delta_time += Time.deltaTime;
        if (this.interval_time <= this.delta_time)
        {
            // デルタタイムを初期化する
            this.delta_time = 0.0f;

            // 投射角度30°のcos,sin値をメンバ変数に取得する。
            this.force_cos_val = Mathf.Cos((float)this.force_angle * Mathf.Deg2Rad);
            this.force_sin_val = Mathf.Sin((float)this.force_angle * Mathf.Deg2Rad);

            // リングとポールの位置を取得する
            Vector3 ring_pos_v3 = this.ring[this.target_ring_index].transform.position;
            Vector3 pole_pos_v3 = this.pole[this.target_pole_index].transform.position;

            // リングからポールまでの距離を取得する
            float distance = distance_ring_to_pole_XZ(ring_pos_v3, pole_pos_v3);
            // リングからポールまでのXZ平面上の角度を取得する
            float angle_XZ = angle_ring_to_pole_XZ(ring_pos_v3, pole_pos_v3);
            // リングを投射する初速度をfloatで取得する
            float velocity = first_velocity(distance);

            // リングを投射する
            // 加える力のベクトルを生成する
            Vector3 force_v3 = this.force_n_v3 * velocity;
            force_v3 = Quaternion.Euler(0, angle_XZ, this.force_angle) * force_v3;
            // 力を加えて放出する. ForceMode.VelocityChange
            GameObject ring = Instantiate(this.ring[this.target_ring_index]);
            ring.GetComponent<Rigidbody>().AddForce(force_v3, ForceMode.VelocityChange);

            // 投射対象のポールのインデックスをインクリメントする
            this.target_pole_index += 1;
            if (this.target_pole_index >= this.pole.Length)
            {
                // 対象ポールのインデックス番号がMAXに達したら0に戻す
                this.target_pole_index = 0;
                this.interval_time = 4.0f;

                // 対象ポールのインデックス番号がMAXに達したら投射角度を変更する
                this.force_angle += 15;
                if (this.force_angle > 60) this.force_angle = 30;
            }
            else
            {
                this.interval_time = 2.0f;
            }

            // 投射対象のリングのインデックスを更新する
            this.target_ring_index += 1;
            if (this.target_ring_index >= this.ring.Length)
            {
                this.target_ring_index = 0;
            }
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        // ポールをメンバ変数(配列)に取得する
        this.pole[0] = GameObject.Find("Pole_r_003_h_050_1");
        this.pole[1] = GameObject.Find("Pole_r_003_h_050_2");
        this.pole[2] = GameObject.Find("Pole_r_003_h_050_3");
        this.pole[3] = GameObject.Find("Pole_r_003_h_050_4");

        // リングをメンバ変数(配列)に取得する
        this.ring[0] = this.ring_prefab_silver;
        this.ring[1] = this.ring_prefab_red;
        this.ring[2] = this.ring_prefab_green;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
