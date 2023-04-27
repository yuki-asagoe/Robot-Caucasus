# コーカサス モータードライバー

## ビルド
*Arduino IDE*経由でビルドします。
六甲おろしモータードライバーにISPで書き込んでください。

ボードごとに変更する必要のある値は`variables.h`に切り出しています。

#### 依存関係
[六甲おろしモータードライバー v1.x 2016](https://github.com/RokkoOroshi/CanMotorBoard2016)

## 利用法
CAN通信を用いて制御します。

送信先アドレスにつきましては`variables.h`定義のIDを用いて構築されます。  
申し訳ないけど詳細[前述のリポジトリ](https://github.com/RokkoOroshi/CanMotorBoard2016)の `can_communication` Libを参照してね

### CANデータ構造
CANメッセージは8byteあります。以下その内訳です。1番モーターを`m1`、2番モーターを`m2`と書きます
|1|2|3|4|
|:-:|:-:|:-:|:-:|
|`m1`回転法(0:停止,+:正転,-:逆転,255:Free)|`m1`出力強度(unsigned)|`m2`回転法(0:停止,+:正転,-:逆転255:Free)|`m2`出力強度(unsigned)|