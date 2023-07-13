const int CAN_Speed=CAN_250KBPS;
const uint8_t CAN_Self_Address=1;

enum class Limit_Switch_Type : int{
  //無効化
  Disabled=0,
  //INPUT[X]_1がHIGHならM[X]を強制停止
  Single=1,
  //INPUT[X]_1,INPUT[X]_2のどちらかがHIGHならM[X]を強制停止
  OR=2
};

const Limit_Switch_Type Limit_Switch_For_M1 = Limit_Switch_Type::Disable;
const Limit_Switch_Type Limit_Switch_For_M2 = Limit_Switch_Type::Disable;
const bool Detailed_Logging_Enabled=true;