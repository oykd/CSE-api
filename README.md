# CSE-api
C# classes for sending async requests to crypto stock exchanges api: Binance, Gate, Huobi, Kucoin, Hitbtc

Classes provides basic and simple way to get any data from api. 
1. Generate string with this structure: "method endpoint parameters" or "method endpoint".
2. Use "sendReq" method to send request (for example: huobi.sendReq("GET /v1/order/orders symbol=ethusdt&states=filled");).
3. You can get response-string on the event "OnResponseReceived" or (if you use threading) in "responseData" after waitingResponse=false.
4. Watch /example/ for better understanding...

Endpoints and parameters available in the api docs:

https://github.com/binance-exchange/binance-official-api-docs

https://github.com/huobiapi/API_Docs_en/wiki

https://kucoinapidocs.docs.apiary.io

https://gate.io/api2

https://api.hitbtc.com/


#### Files:
apiLib/someApi.cs
  - Main parent class. Contains various types of requests, helpers and some other unnecessary functions
  
apiLib/API/binance.cs 

apiLib/API/gate.cs 

apiLib/API/huobi.cs 

apiLib/API/kucoin.cs 

apiLib/API/hitbtc.cs 

  - Stock exchanges child classes. Contains specific encryption function and pre-request parameters-string manipulations. 

apiLib/API/pattern.cs
  - Pattern for fast adding new stock exchange api-class. Nothing intresting. 

example/
  - Console application for simple tests. Just compile&run it. 





Feel free to contact & ask any questions --

  skype: stimpackuser
  
  email: d-yoda@yandex.ru
