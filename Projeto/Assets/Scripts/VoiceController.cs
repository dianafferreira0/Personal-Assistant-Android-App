using System.Collections;
using System.Collections.Generic;
using TextSpeech;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using Random=UnityEngine.Random;
using System;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

public class VoiceController : MonoBehaviour
{
    //Câmara foto
    const string ACTION_STILL_IMAGE_CAMERA = "android.media.action.STILL_IMAGE_CAMERA";
    public void OpenCamera(){
        var intentAJOCAM = new AndroidJavaObject("android.content.Intent", ACTION_STILL_IMAGE_CAMERA);
        GetUnityActivity().Call("startActivity", intentAJOCAM);
    }

    //Câmara video
    const string ACTION_VIDEO_CAMERA = "android.media.action.VIDEO_CAMERA";
    public void OpenCameraVideo(){
        var intentAJOCV = new AndroidJavaObject("android.content.Intent", ACTION_VIDEO_CAMERA);
        GetUnityActivity().Call("startActivity", intentAJOCV);
    }

    //Chamar táxi
    const string ACTION_RESERVE_TAXI = "com.google.android.gms.actions.RESERVE_TAXI_RESERVATION";
    public void ChamarCarro(){
        var intentAJOC = new AndroidJavaObject("android.content.Intent", ACTION_RESERVE_TAXI);
        GetUnityActivity().Call("startActivity", intentAJOC);
    }

    //Criar Contacto
    //const string ACTION_CREATE_CONTACTO = "android.intent.action.INSERT";
    const string ACTION_INSERT = "android.intent.action.INSERT";
    const string CONTACTS_CONTENT_TYPE = "vnd.android.cursor.dir/contact";
    const string ACTION_CREATE_CONTACTO_EXTRA_NOME = "name";
    const string ACTION_CREATE_CONTACTO_EXTRA_PHONE = "phone";
    public void CreateContacto(string nome, string numero){
        var intentAJOCC = new AndroidJavaObject("android.content.Intent", ACTION_INSERT);
        intentAJOCC
            .Call<AndroidJavaObject>("setType", CONTACTS_CONTENT_TYPE)
            .Call<AndroidJavaObject>("putExtra", ACTION_CREATE_CONTACTO_EXTRA_NOME, nome)
            .Call<AndroidJavaObject>("putExtra", ACTION_CREATE_CONTACTO_EXTRA_PHONE, numero);
        GetUnityActivity().Call("startActivity", intentAJOCC);
    }

    //Calculadora
    const string ACTION_CALC = "android.intent.category.APP_CALCULATOR";
    const string ACTION_MAIN = "android.intent.action.MAIN";
    public void AbreCalculadora(){
        var intentAJOCALC = new AndroidJavaObject("android.content.Intent");
        intentAJOCALC
            .Call<AndroidJavaObject>("setAction", ACTION_MAIN)
            .Call<AndroidJavaObject>("addCategory", ACTION_CALC)
            .Call<AndroidJavaObject>("setFlags", 268435456);

        GetUnityActivity().Call("startActivity", intentAJOCALC);
    }

    //Galeria
    const string ACTION_GALLERY = "android.intent.category.APP_GALLERY";
    public void AbreGaleria(){
        var intentAJOGALL = new AndroidJavaObject("android.content.Intent");
        intentAJOGALL
            .Call<AndroidJavaObject>("setAction", ACTION_MAIN)
            .Call<AndroidJavaObject>("addCategory", ACTION_GALLERY)
            .Call<AndroidJavaObject>("setFlags", 268435456);

        GetUnityActivity().Call("startActivity", intentAJOGALL);
    }

    //Calendário
    const string ACTION_CALENDAR = "android.intent.category.APP_CALENDAR";
    public void AbreCalendario(){
        var intentAJOCALL = new AndroidJavaObject("android.content.Intent");
        intentAJOCALL
            .Call<AndroidJavaObject>("setAction", ACTION_MAIN)
            .Call<AndroidJavaObject>("addCategory", ACTION_CALENDAR)
            .Call<AndroidJavaObject>("setFlags", 268435456);

        GetUnityActivity().Call("startActivity", intentAJOCALL);
    }

    //Mensagens
    const string ACTION_MESSAGES = "android.intent.category.APP_MESSAGING";
    public void AbreMensagens(){
        var intentAJOMSSG = new AndroidJavaObject("android.content.Intent");
        intentAJOMSSG
            .Call<AndroidJavaObject>("setAction", ACTION_MAIN)
            .Call<AndroidJavaObject>("addCategory", ACTION_MESSAGES)
            .Call<AndroidJavaObject>("setFlags", 268435456);

        GetUnityActivity().Call("startActivity", intentAJOMSSG);
    }

    //Notas
    const string ACTION_CREATE_NOTE = "com.google.android.gms.actions.CREATE_NOTE";
    const string ACTION_CREATE_NOTE_NOTE = "com.google.android.gms.actions.extra.NAME";
    const string ACTION_CREATE_NOTE_TEXT = "com.google.android.gms.actions.extra.TEXT";
    public void CriarNota(string nome, string nota){
        var intentAJONOTA = new AndroidJavaObject("android.content.Intent", ACTION_CREATE_NOTE);
            intentAJONOTA
            .Call<AndroidJavaObject>("setType", "*/*")
            .Call<AndroidJavaObject>("putExtra", ACTION_CREATE_NOTE_NOTE, nome)
            .Call<AndroidJavaObject>("putExtra", ACTION_CREATE_NOTE_TEXT, nota);
        GetUnityActivity().Call("startActivity", intentAJONOTA);
    }

    //Mapa
    float latitudeMapa;
    float longitudeMapa;
    IEnumerator GetLocation()
    {
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
            yield break;
        // Start service before querying location
        Input.location.Start();
        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }
        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            yield break;
        }
        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            
            yield break;
        }
        else
        {
            latitudeMapa = Input.location.lastData.latitude;
            longitudeMapa = Input.location.lastData.longitude;

            //print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
        }
        // Stop service if there is no need to query location updates continuously
        Input.location.Stop();
    }

    //Alarme
    const string ACTION_SET_ALARM = "android.intent.action.SET_ALARM";
    const string EXTRA_HOUR = "android.intent.extra.alarm.HOUR";
    const string EXTRA_MINUTES = "android.intent.extra.alarm.MINUTES";
    const string EXTRA_MESSAGE = "android.intent.extra.alarm.MESSAGE";

    public void CreateAlarm(string nomeAlarme, int hora, int min){
        var intentAJO = new AndroidJavaObject("android.content.Intent", ACTION_SET_ALARM);
        intentAJO
            .Call<AndroidJavaObject>("putExtra", EXTRA_MESSAGE, nomeAlarme)
            .Call<AndroidJavaObject>("putExtra", EXTRA_HOUR, hora)
            .Call<AndroidJavaObject>("putExtra", EXTRA_MINUTES, min);
        GetUnityActivity().Call("startActivity", intentAJO);
    }

    AndroidJavaObject GetUnityActivity(){
        using(var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")){
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }
    }

    //METEOROLOGIA 
    public string apiKey = "0272630079589bb6d83c0cc4063ce7be";
	public string city;
	public bool useLatLng = false;
	public string latitude;
	public string longitude;
    public string temperatura;
    
	public void GetRealWeather () {
		string uri = "api.openweathermap.org/data/2.5/weather?";
		if (useLatLng) {
			uri += "lat=" + latitude + "&lon=" + longitude + "&appid=" + apiKey + "&lang=pt";
		} else {
			uri += "q=" + city + "&appid=" + apiKey + "&lang=pt";
		}
		StartCoroutine (GetWeatherCoroutine (uri));
	}

	IEnumerator GetWeatherCoroutine (string uri) {
		using (UnityWebRequest webRequest = UnityWebRequest.Get (uri)) {
			yield return webRequest.SendWebRequest ();
			if (/*webRequest.isNetworkError*/ webRequest.result == UnityWebRequest.Result.ConnectionError) {
				Debug.Log ("Web request error: " + webRequest.error);
			} else {
				ParseJson (webRequest.downloadHandler.text);
			}
		}
	}
    
	WeatherStatus ParseJson (string json) {
		WeatherStatus weather = new WeatherStatus ();
		try {
        
			dynamic obj = JObject.Parse (json);

			weather.weatherId = obj.weather[0].id;
			weather.main = obj.weather[0].main;
			weather.description = obj.weather[0].description;
			weather.temperature = obj.main.temp;
			weather.pressure = obj.main.pressure;
			weather.windSpeed = obj.wind.speed;
		} catch (Exception e) {
			Debug.Log (e.StackTrace);
		}
		
		Debug.Log ("Temp in °C: " + weather.Celsius ());
		Debug.Log ("Wind speed: " + weather.windSpeed);
        float celsius;
        weather.pressure = weather.pressure / 1000;
        celsius = weather.Celsius();

        temperatura = "Meteorologia para " + city + ". " + "Tempo: " + weather.description.ToString() + ". " + "Temperatura: " + celsius + "graus Celsius. ";
        /* + "Pressao: " + weather.pressure.ToString() + " Bar. " + "Velocidade do vento: " + weather.windSpeed.ToString() + "metros por segundo";*/
        return weather;
	}

    //Início
    const string LANG_CODE = "pt-PT";

    [SerializeField]
    Text uiText;    
     
    [SerializeField]
    Text uiText2;
    void Start(){
        SetUp(LANG_CODE);
        #if UNITY_ANDROID
            SpeechToText.instance.onPartialResultsCallback = OnPartialSpeechResult;
        #endif
        SpeechToText.instance.onResultCallback = OnFinalSpeechResult;
        TextToSpeech.instance.onStartCallBack = OnSpeakStart;
        TextToSpeech.instance.onDoneCallback = OnSpeakStop;
        CheckPermission();
    }
    void CheckPermission(){
        #if UNITY_ANDROID
            if(!Permission.HasUserAuthorizedPermission(Permission.Microphone)){
                Permission.RequestUserPermission(Permission.Microphone);
            }
        #endif
    }

    #region Text to Speech

    public void StartSpeakingMeteorologia(string tempo){
        TextToSpeech.instance.StartSpeak(tempo);
    }
    public void StartSpeakingHoras(){
        string time = DateTime.Now.ToString("hh:mm");
        TextToSpeech.instance.StartSpeak(time);
    }
    public void StartSpeakingSuporte(){
        TextToSpeech.instance.StartSpeak("Para mais informações, contacte o: 912 345 678");
    }
    public void StartSpeakingGreet(){
        int randomNum = Random.Range(1,5);
        if(randomNum == 1)
        TextToSpeech.instance.StartSpeak("Olá");
        if(randomNum == 2)
        TextToSpeech.instance.StartSpeak("Tudo bem?");
        if(randomNum == 3)
        TextToSpeech.instance.StartSpeak("Viva");
        if(randomNum == 4)
        TextToSpeech.instance.StartSpeak("Como vai isso?");
    }
    public void StartSpeakingBattery(float nivelBateria){
        nivelBateria = nivelBateria * 100;
        string nivelBateriaString = nivelBateria.ToString();
        if(nivelBateria == -1){
            TextToSpeech.instance.StartSpeak("Nivel de bateria nao encontrado");
        }
        TextToSpeech.instance.StartSpeak(nivelBateriaString + " %");
    }
    public void StartSpeakingModelo(){
        TextToSpeech.instance.StartSpeak(SystemInfo.deviceModel);
    }
    public void StartSpeakingTipo(){
        if(SystemInfo.deviceType.ToString() == "Unknown")
        TextToSpeech.instance.StartSpeak("Tipo de dispositivo desconhecido");
        if(SystemInfo.deviceType.ToString() == "Handheld")
        TextToSpeech.instance.StartSpeak("Está a usar um telemóvel");
        if(SystemInfo.deviceType.ToString() == "Console")
        TextToSpeech.instance.StartSpeak("Está a usar uma consola");
        if(SystemInfo.deviceType.ToString() == "Desktop")
        TextToSpeech.instance.StartSpeak("Está a usar um computador");
    }
    public void StartSpeakingSO(){
        TextToSpeech.instance.StartSpeak(SystemInfo.operatingSystem);
    }
    public void StartSpeakingEstado(){
        if(SystemInfo.batteryStatus.ToString() == "Unknown")
        TextToSpeech.instance.StartSpeak("Estado de bateria desconhecido");
        if(SystemInfo.batteryStatus.ToString() == "Charging")
        TextToSpeech.instance.StartSpeak("O telemóvel está a carregar");
        if(SystemInfo.batteryStatus.ToString() == "Discharging")
        TextToSpeech.instance.StartSpeak("O telemóvel não está a carregar");
        if(SystemInfo.batteryStatus.ToString() == "NotCharging")
        TextToSpeech.instance.StartSpeak("O telemóvel não está a carregar");
        if(SystemInfo.batteryStatus.ToString() == "Full")
        TextToSpeech.instance.StartSpeak("A bateria do telemóvel está cheia");
    }
    public void StartSpeaking(Text uiText2){
        TextToSpeech.instance.StartSpeak(uiText2.text);
    }
    public void StopSpeaking(){
        TextToSpeech.instance.StopSpeak();
    }
    void OnSpeakStart(){
        Debug.Log("Talking started");
    }
    void OnSpeakStop(){
        Debug.Log("Talking stopped");
    }

    #endregion

    #region Speech To Text

    public void StartListening(){
        SpeechToText.instance.StartRecording();
    }
    public void StopListening(){
        SpeechToText.instance.StopRecording();
    }
    void OnFinalSpeechResult(string result){
        uiText.text = result;
        
        string questao = result.Split(' ')[0];
        if(questao == "meteorologia" || questao == "tempo" || questao == "Meteorologia" || questao == "Tempo"){
            city = result.Split(' ')[1];
            if(city != null){
                GetRealWeather();
                string meteorologia = temperatura;
                StartSpeakingMeteorologia(meteorologia);
            }
        }
        if(result.Contains("mensagem")){
            string mobile_num = "";
            string message = "";

            string[] resultadoMssg = result.Split(' ');
            foreach(var word in resultadoMssg){
                if(word == "mensagem"){

                }else
                mobile_num = mobile_num + word;
            }

            #if UNITY_ANDROID
            string URL = string.Format("sms:{0}?body={1}",mobile_num,System.Uri.EscapeDataString(message));
            #endif

            Application.OpenURL(URL);
        }if(result.Contains("chamada")){
            string[] resultado2 = result.Split(' ');
            string numeroTel ="";
            foreach(var word in resultado2){
                if(word == "chamada"){

                }else
                numeroTel = numeroTel + word;
            }
            string urlTel = string.Format("tel://{0}", numeroTel);
            Application.OpenURL(urlTel);
        }if(result.Contains("horas") || result.Contains("hora")){
            StartSpeakingHoras();
        }if(result.Contains("YouTube")){
            Application.OpenURL("https://youtube.com");
        }if(result.Contains("Facebook")){
            Application.OpenURL("fb://page/--------");
        }if(result.Contains("Instagram")){
            Application.OpenURL("https://instagram.com");
        }if(result.Contains("e-mail")){
            Application.OpenURL("mailto://");
        }if(result.Contains("Google Play")){
            Application.OpenURL("https://play.google.com/store");
        }if(result.Contains("pesquisar") || result.Contains("Pesquisar") || result.Contains("pesquisa") || result.Contains("Pesquisa")){
            string[] resultadoPesquisar = result.Split(' ');
            string itemPesquisado ="";
            foreach(var word in resultadoPesquisar){
                if(word == "pesquisar" || word == "Pesquisar" || word == "Pesquisa" || word == "pesquisa"){

                }else
                itemPesquisado = itemPesquisado + word + " ";
            }
            string urlPesquisa = string.Format("https://www.google.pt/search?q={0}", itemPesquisado);
            Application.OpenURL(urlPesquisa);
        }if(result.Contains("Notícias") || result.Contains("notícias")){
            Application.OpenURL("https://www.jn.pt/");
        }if(result.Contains("Messenger")){
            //Application.OpenURL("fb-messenger://");
            Application.OpenURL("https://m.me");
        }if(result.Contains("ajuda") || result.Contains("Ajuda")){
            Application.OpenURL("https://github.com/VitorCoelhoNeto/ProjetosUniversidade/blob/main/ajudaDidimo");
        }if(result.Contains("suporte") || result.Contains("Suporte")){
            StartSpeakingSuporte();
        }if(result.Contains("pandemia") || result.Contains("Pandemia") || result.Contains("Ponto de situação covid-19") || result.Contains("ponto de situação covid-19")){
            Application.OpenURL("https://covid19.min-saude.pt/ponto-de-situacao-atual-em-portugal/");
        }if(result.Contains("alarme") || result.Contains("Alarme")){
            string[] resultadoAlarme = result.Split(' ');
            string horasEMinutos = "";
            string mensagemAlarme = "";
            foreach(var word in resultadoAlarme){
                if(word == "alarme" || word == "Alarme"){
                    mensagemAlarme = word;
                }else
                horasEMinutos = horasEMinutos + word;
            }

            var pieces = horasEMinutos.Split(new[] { ':' }, 2);
	
		    string horaString = pieces[0];
            string minutoString = pieces[1];
            int hora;
            int minuto;

            hora = Int32.Parse(horaString);
            minuto = Int32.Parse(minutoString);

            CreateAlarm(mensagemAlarme, hora, minuto);
        }if(result.Contains("olá") || result.Contains("Olá")){
            StartSpeakingGreet();
        }if(result.Contains("câmara") || result.Contains("Câmara") || result.Contains("câmera") || result.Contains("Câmera")){
            OpenCamera();
        }if(result.Contains("vídeo") || result.Contains("Vídeo")){
            OpenCameraVideo();
        }if(result.Contains("mapa") || result.Contains("Mapa") || result.Contains("GPS")){
            string URLMapa = "";
            GetLocation();
            URLMapa = string.Format("geo:{0},{1}", latitudeMapa, longitudeMapa);
            Application.OpenURL(URLMapa);
        }if(result.Contains("carro") || result.Contains("Carro")){ //Apenas disponível para Wear OS (Relógio da Google)
            ChamarCarro();
        }if(result.Contains("contacto") || result.Contains("Contacto") || result.Contains("Contato") || result.Contains("contato")){
            string[] resultadoContactoNome = result.Split(' ');
            string numero = "";
            foreach(var word in resultadoContactoNome){
                if(word == "contacto" || word == "Contacto" || word == "Contato" || word == "contato" || word == resultadoContactoNome[1]){}
                else
                numero = numero + word;
            }
            CreateContacto(resultadoContactoNome[1], numero);
        }if(result.Contains("calculadora") || result.Contains("Calculadora")){
            AbreCalculadora();
        }if(result.Contains("mensagens") || result.Contains("Mensagens")){
            AbreMensagens();
        }if(result.Contains("galeria") || result.Contains("Galeria") || result.Contains("fotos") || result.Contains("Fotos")){
            AbreGaleria();
        }if(result.Contains("calendário") || result.Contains("Calendário") || result.Contains("agenda") || result.Contains("Agenda")){
            AbreCalendario();
        }

        //Fase de Testes
        if(result.Contains("nota") || result.Contains("Nota") || result.Contains("notas") || result.Contains("Notas")){
            string[] resultadoNota = result.Split(' ');
            string nota = "";

            foreach(var word in resultadoNota){
                if(word == resultadoNota[0] || word == resultadoNota[1]){}
                else
                nota = nota + word + " ";
            }
            CriarNota(resultadoNota[1], nota);
        }
        
        //Extras
        if(result.Contains("bateria") || result.Contains("Bateria")){
            float nivelBateria = SystemInfo.batteryLevel;
            StartSpeakingBattery(nivelBateria);
        }if(result.Contains("modelo") || result.Contains("Modelo")){
            StartSpeakingModelo();
        }if(result.Contains("tipo") || result.Contains("Tipo")){
            StartSpeakingTipo();
        }if(result == "sistema operativo"){
            StartSpeakingSO();
        }if(result.Contains("estado") || result.Contains("Estado")){
            StartSpeakingEstado();
        }
    }
    void OnPartialSpeechResult(string result){
        uiText.text = result;
    }

    #endregion
    void SetUp(string code){
        TextToSpeech.instance.Setting(code, 1, 1);
        SpeechToText.instance.Setting(code);
    }
}