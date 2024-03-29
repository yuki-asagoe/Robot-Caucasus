#include <mcp_can.h>
#include <can_communication.h>
#include<Wire.h>
#include<Adafruit_PWMServoDriver.h>

#include "variables.h"

void on_receive_can(uint16_t, const int8_t*, uint8_t);
void on_error_occur(uint8_t,uint8_t);
void set_servo_angle(uint8_t, int);
void set_servo_pulse(uint8_t, double);
void warn(char*);

Adafruit_PWMServoDriver pwm = Adafruit_PWMServoDriver();
unsigned long long last_can_timestamp=0;
unsigned long long last_error_timestamp=0;

void setup(){
  Serial.begin(115200);
  pinMode(STAT_LED1, OUTPUT); // CAN メッセージ受信したら光る
  pinMode(STAT_LED2, OUTPUT); // CAN制御信号が1秒途切れたら光る
  pinMode(SERVO_OFF, OUTPUT);
  digitalWrite(SERVO_OFF, LOW);

  pwm.begin();
  pwm.setPWMFreq(PWM_Frequency); //50Hz

  CanCom.begin(CAN_Self_Address, CAN_Speed); 
  CanCom.setReceiveFilter(true);
  CanCom.onReceive(on_receive_can);

  for(int i=0;i<8;i++){
    set_servo_angle(i,Servo_Initial_Angle[i]);
  }

  Serial.println("Ready to Drive - servo driver on Caucasus");
  Serial.print("Can destination ID is\"");
  Serial.print(CAN_Self_Address,DEC);
  Serial.println("\". Git Repository - https://github.com/yuki-asagoe/Robot-Caucasus");
}

void loop(){
  //データ受信を確認し必要ならonReceiveで登録したリスナを呼び出す
  CanCom.tasks();
  unsigned long long now=millis();
  if(now - last_error_timestamp < 1000){ // 直近のエラーから1秒以内
    if((long)((now-last_error_timestamp)/100.0) & 1 == 1){// 100ms 間隔
      digitalWrite(STAT_LED2,HIGH);
    }else{
      digitalWrite(STAT_LED2,LOW);
    }
  }
  else if(now - last_can_timestamp > 1000){ //信号途絶
    digitalWrite(STAT_LED2,HIGH);
  }else{
    digitalWrite(STAT_LED2,LOW);    
  }
  if(now -last_can_timestamp < 100){
    digitalWrite(STAT_LED1,HIGH);
  }else{
    digitalWrite(STAT_LED1,LOW);    
  }
}

void on_receive_can(uint16_t std_id, const int8_t *data, uint8_t len) {
  last_can_timestamp=millis();
  uint8_t msg_type = CanCommunication::getDataTypeFromStdId(std_id);
  uint8_t dest = CanCommunication::getDestFromStdId(std_id);  
  
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
  
  switch(msg_type){
    case CAN_DATA_TYPE_COMMAND:{
      int skip_count=0;
      int reset_count=0;
      for(uint8_t i=0;i<len;i++){
        uint8_t angle=(uint8_t)data[i];
        if(angle==255){//値255は無視
          skip_count++;
          Serial.println("Control Skipped");
          continue;
        }
        if(angle==254){
          reset_count++;
          Serial.println("Angle Reset");
          set_servo_angle(i,Servo_Initial_Angle[i]);
          continue;
        }
        Serial.print("Angle set : ");
        Serial.print(angle);
        Serial.println("");
        set_servo_angle(i,angle);
      }
      if(skip_count>=len||reset_count>=len){
        Serial.println("Reboot Servos");      
        digitalWrite(SERVO_OFF, HIGH);
        for(int i=0;i<len;i++){
          set_servo_angle(i,Servo_Initial_Angle[i]);
        }
        delay(1000);
        digitalWrite(SERVO_OFF, LOW);
      }
      break;
    }
    case CAN_DATA_TYPE_EMERGENCY:{
      Serial.println("Emergency Code Detected");
      break;
    }
    Serial.println("");
  }
}

void on_error_occur(uint8_t interrupted,uint8_t error_status){
  last_error_timestamp=millis();
  Serial.print("CAN Error: CODE[");
  Serial.print(error_status,HEX);
  Serial.println("]");
}

void warn(char* msg){
  Serial.print("Warning: ");
  Serial.print(msg);
  Serial.print(" -by servo drive(id:");
  Serial.print(CAN_Self_Address,DEC);
  Serial.println(")");
}

void set_servo_angle(uint8_t n, int deg) {
  if(deg < 0) deg = 0;
  if(deg > Servo_Max_Angle[n]) deg = Servo_Max_Angle[n];
  double width = Servo_Max_Pulse_Width[n] - Servo_Min_Pulse_Width[n]; //パルス幅の変域の大きさ、ミリ秒
  double pulse = (((double)deg) / Servo_Max_Angle[n]) * width + Servo_Min_Pulse_Width[n]; //やってることは単なる線形補完のようなもの
  set_servo_pulse(n, pulse);
}

// pulse の時間単位はマイクロ秒
// 周期中に指定したミリ秒の出力を行うように命令する。
void set_servo_pulse(uint8_t n, double pulse) {
  double pulselength = 1000000;   // 1,000,000 マイクロ秒/秒
  pulselength /= PWM_Frequency;   // pwm一周期のマイクロ秒
  pulselength /= 4096;  // 4096 tickで一周期をカウントするとして1ティックあたりのマイクロ秒
  pulse /= pulselength; // 指定されたパルス幅を表現できるティック数の取得
  pwm.setPWM(n, 0, pulse); // ティック数の指定
}