const int CAN_Speed=CAN_250KBPS;
const int PWM_Frequency=50; //単位はHz
const uint8_t CAN_Self_Address=6;

// 以下時間単位マイクロ秒
// サーボが最大角度をとるときのpwm一周期に対する出力時間
const double Servo_Max_Pulse_Width[] = {2500,2500,2500,2500,2500,2500,2500,2500};
// サーボが最大角度をとるときのpwm一周期に対する出力時間
const double Servo_Min_Pulse_Width[] = {500,500,500,500,500,500,500,500};
// サーボの初期角度
const int Servo_Initial_Angle[]={90,90,90,90,0,0,0,0};
// サーボの最大角度(これはサーボの性能としての最大角度で、実際の可動域ではない)
const int Servo_Max_Angle[]={180,180,180,180,180,180,180,180};