#include <mcp_can.h>
#include <motor.h>
#include <can_communication.h>

#include "variables.h"

void on_receive_can(uint16_t, const int8_t*, uint8_t);
void warn(char*);
bool is_limited_motor_of(int number);

unsigned long long last_can_timestamp=0;
unsigned long long limit_switch_output_timer_1=0;
unsigned long long limit_switch_output_timer_2=0;

void setup(){
  Serial.begin(115200);
  pinMode(STAT_LED1, OUTPUT); // CAN メッセージ受信したら光る
  pinMode(STAT_LED2, OUTPUT); // CAN制御信号が1秒途切れ(てセーフティが発動し)たら光る

  if(Limit_Switch_For_M1_Enabled){
    //INPUT1の1番(基盤内側)のピン M1用リミットスイッチ LOW(GND)入力で平常時、HIGH(VCC)入力で強制モーター停止
    pinMode(INPUT1_1,INPUT_PULLUP);
  }
  if(Limit_Switch_For_M2_Enabled){
    //INPUT1の2番(基盤外側)のピン M2用リミットスイッチ
    pinMode(INPUT1_2,INPUT_PULLUP);
  }
  motorInit();

  CanCom.begin(CAN_Self_Address, CAN_Speed);
  CanCom.onReceive(on_receive_can);

  Serial.println("Ready to Drive - motor driver on Caucasus");
  Serial.print("CAN destination ID is\"");
  Serial.print(CAN_Self_Address,DEC);
  Serial.println("\".");
  if(Limit_Switch_For_M1_Enabled){
    Serial.println("This board is accepting a limit switch input for M1 with INPUT1_1 pin.");
  }
  if(Limit_Switch_For_M2_Enabled){
    Serial.println("This board is accepting a limit switch input for M2 with INPUT1_2 pin.");
  }
  Serial.println("Git Repository - https://github.com/yuki-asagoe/Robot-Caucasus");
}

void loop(){
  //データ受信を確認し必要ならonReceiveで登録したリスナを呼び出す
  CanCom.tasks();
  unsigned long long now=millis();
  if(now - last_can_timestamp > 1000){ //セーフティストッパー
    motorStop(1);
    motorStop(2);
    digitalWrite(STAT_LED2,HIGH);
  }else{
    digitalWrite(STAT_LED2,LOW);    
  }
  if(now -last_can_timestamp < 100){
    digitalWrite(STAT_LED1,HIGH);
  }else{
    digitalWrite(STAT_LED1,LOW);    
  }
  if(is_limited_motor_of(1)){
    ///300秒間隔でリミットスイッチ押下のフィードバック
    if(now - limit_switch_output_timer_1>300){
      Serial.println("M1:Stopping - Reaching Limit");
      limit_switch_output_timer_1=now;
    }
    motorStop(1);
  }
  if(is_limited_motor_of(2)){
    if(now - limit_switch_output_timer_2>300){
      Serial.println("M2:Stopping - Reaching Limit");
      limit_switch_output_timer_2=now;
    }
    motorStop(2);
  }
}

void on_receive_can(uint16_t std_id, const int8_t *data, uint8_t len) {
  last_can_timestamp=millis();
  uint8_t msg_type = CanCommunication::getDataTypeFromStdId(std_id);
  uint8_t dest = CanCommunication::getDestFromStdId(std_id);  
  
  if(Detailed_Logging_Enabled){
    Serial.println("Received Data");
    Serial.print("ID-dest:");
    Serial.print(dest,HEX);
    Serial.print(" / ID-msg-type:");
    Serial.print(msg_type,HEX);
    Serial.print(" / Length:");
    Serial.print(len,HEX);
    Serial.print(" / Data:");
    for(int i=0;i<len;i++){
      Serial.print(data[i],HEX);
      Serial.print(" ");
    }
    Serial.println("");
  }
  
  switch(msg_type){
    case CAN_DATA_TYPE_COMMAND:{
      if(len!=4){
        warn("illegal data length");
        return;
      }
      //M1のパース
      if(is_limited_motor_of(1)){
        Serial.println("M1:Stopping - Reaching Limit");
        motorStop(1);
      }
      else if(data[0]==0){
        Serial.println("M1:Stopping");
        motorStop(1);
      }
      else if(data[0]==-128){
        Serial.println("M1:Free");
        motorFree(1);
      }
      else if(data[0]>0){
        Serial.print("M1:Driving / ");
        Serial.print((uint8_t)data[1],DEC);
        Serial.println("");        
        motorWrite(1,(uint8_t)data[1]);
      }
      else {
        Serial.print("M1:Driving / -");
        Serial.print((uint8_t)data[1],DEC);
        Serial.println("");        
        motorWrite(1,-(uint8_t)data[1]);
      }

      //M2のパース
      if(is_limited_motor_of(2)){
        Serial.println("M2:Stopping - Reaching Limit");
        motorStop(2);
      }
      else if(data[2]==0){
        Serial.println("M2:Stopping");
        motorStop(2);
      }
      else if(data[2]==-128){
        Serial.println("M2:Free");
        motorFree(2);
      }
      else if(data[2]>0){
        Serial.print("M2:Driving / ");
        Serial.print((uint8_t)data[1],DEC);
        Serial.println("");       
        motorWrite(2,(uint8_t)data[3]);
      }
      else {
        Serial.print("M2:Driving / -");
        Serial.print((uint8_t)data[1],DEC);
        Serial.println("");       
        motorWrite(2,-(uint8_t)data[3]);
      }
      break;      
    }
    case CAN_DATA_TYPE_EMERGENCY:{
      Serial.println("Emergency Code Detected : All Motors are stopping");
      motorStop(1);
      motorStop(2);
      break;
    }
    Serial.println("");
  }
}

void warn(char* msg){
  Serial.print("Warning: ");
  Serial.print(msg);
  Serial.print(" -by motor drive(id:");
  Serial.print(CAN_Self_Address,DEC);
  Serial.println(")");
}

bool is_limited_motor_of(int number){
  switch(number){
    case 1:
      if(Limit_Switch_For_M1_Enabled) return digitalRead(INPUT1_1) == HIGH;
      else return false;
    case 2:
      if(Limit_Switch_For_M2_Enabled) return digitalRead(INPUT1_2) == HIGH;
      else return false;
  }
  warn("Illegal Motor Number in func is_limited_motor_of");
  return true;
}