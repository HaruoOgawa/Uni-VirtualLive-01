# Uni-VirtualLive-01
[![IMAGE ALT TEXT HERE](https://github.com/user-attachments/assets/3c5f4bd3-f339-4d6b-b817-62903d466c54)](https://www.youtube.com/watch?v=7p1aDVnQgqk)
## 説明
演者のモデル・モーション・音源は再配布禁止となっているのでgitには含めていません。
## 導入方法
1. リポジトリをクローン
2. https://bowlroll.net/file/155105 よりPMXをダウンロード
3. https://bowlroll.net/file/194433 よりVMDをダウンロード
4. https://music.apple.com/jp/album/%E5%8A%A3%E7%AD%89%E4%B8%8A%E7%AD%89-feat-%E9%8F%A1%E9%9F%B3%E3%83%AA%E3%83%B3-%E3%83%AC%E3%83%B3-single/1536605729 で曲を購入
5. Unityで当リポジトリのプロジェクトを開く
6. Assets/Live/Scene/Live.unityのシーンファイルを開く
7. 購入した曲をUnityのAssets/Live/Untrackedにインポートする
<img width="1903" height="609" alt="image" src="https://github.com/user-attachments/assets/710260af-c8b7-45d4-a599-9f04c7c30c69" />

8. シーン上のWorld/StageがタイムラインのPlayable Directorを持っているので、その一番上のAUdio TrackのAudio Playable TrackがMissingになっているので曲のアセットをアサインする
9.  ダウンロードしたPMXの「Sour式鏡音レンVer.2.01/Black.pmx」をAssets/Live/Untrackedにドラッグアンドドロップ
10. ダウンロードしたPMXの「Sour式鏡音レンVer.2.01/White.pmx」をAssets/Live/Untrackedにドラッグアンドドロップ
11. ダウンロードしたVMDの「モーション：劣等上等\モーション：劣等上等\1.原曲音源\1-1_motion_TdaAppendLen.vmd」をAssets/Live/Untrackedにドラッグアンドドロップ
12. Unityにてドラッグアンドドロップにより生成された「Assets/Live/Untracked/1-1_motion_TdaAppendLen/1-1_motion_TdaAppendLen.anim」のアニメーションクリップをクリック。インスペクターの「VMR Retarget Tools」のGameObjectに「Assets/Live/Untracked/Black/Prefab/Black.prefab」プレファブを渡し、Retagrt BoneAnimationを実行
13. 同様に「Assets/Live/Untracked/White/Prefab/White.prefab」を渡し、もう一度Retagrt BoneAnimationを実行
14. するとそれぞれのPMXに適したアニメーションクリップがリターゲットしたうえで生成される
15. 先ほどのBlack.prefabとWhite.prefabをシーンに配置し、リターゲットで生成されたアニメーションをドラッグアンドドロップでアサインする
    * トランスフォームは以下のように設定する
      * Black.prefab
        * Position: (-0.5, 0.875, 0.0)
        * Rotation: (0, 0, 0)
        * Scale: (0.1, 0.1, 0.1)
      * White.prefab
        * Position: (0.5, 0.875, 0.0)
        * Rotation: (0, 0, 0)
        * Scale: (0.1, 0.1, 0.1)  
16. タイムラインをもう一度開くとアニメーショントラックがMissingになっているのでシーンに配置した演者のモデルをトラックに渡す。Animation Playable AssetもMissingになっているのでリターゲットで生成したアニメーションクリップを渡す。この作業をBlackとWhite両方のモデルに対して行う
<img width="1565" height="688" alt="image" src="https://github.com/user-attachments/assets/68892c0e-0428-4f83-861a-2131e66267e0" />

17. クローンしたリポジトリの直下に「Uni-VirtualLive-01\TouchDesigner\DMXSender.toe」というTouchDesignerのファイルがあるのでこれを開いて実行する。開いたら「1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 4」の順にキーを押して演出をリセットする
18.  UnityをGameViewで実行
