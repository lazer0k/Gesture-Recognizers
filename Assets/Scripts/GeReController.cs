using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class GeReController : MonoBehaviour
    {
        //Инициализация переменных в сцене
        public Button StartBtn;
        public GameObject Options;
        public Text ScoreText;
        public Text TimerText;
        public Text AccuracyText;
        public Transform LinesParantTransform;
        public Transform PointParentTransform;
        public Image AccuracyResultImage;
        public Sprite CircleSprite;
        public List<Sprite> AccuracyResultSprites = new List<Sprite>();

        //Инициализация переменных - настроек
        public bool WithCursor = true;
        public List<float> TimeForTimer = new List<float>() { 15000, 10000, 5000, 2000, 1000, 500 };
        public float OptionAccuracyResize = 0.05f;
        public float OptionAccuracyValue = 75f;
        public int CurrentGestureId = 0;

        // Ссылка на клас с шаблонами
        private GestureFolder _gestureFolder;

        // Список всех гейм-обьектов, что входят в наш рисунок (для удаления)
        private List<GameObject> _drawedGO = new List<GameObject>();

        // Список точек - нашего рисунка ( для определения процента схожести рисунка с шаблоном )
        private List<GameObject> _drawedGesture = new List<GameObject>();

        // Центральная точка нашего рисунка
        private GameObject _centralPoint;

        // Первая и последния точка координат нажатой мышки
        private Vector2 _lastMouseCood;
        private Vector2 _firstMouseCood;

        // Список всех гейм-обьектов, что входят в шаблон (для удаления)
        private List<GameObject> _gestureGO = new List<GameObject>();

        // Динамичные parent-ы
        private Transform _defultGestureTransform;
        private Transform _defultCircleFolder;

        // Ссылка на список шаблонов
        private List<GestureModel> _allDefaultGesture;

        // Текущий раунд
        private int _currentRound = 0;

        // Секунды для отсчёта
        private float _timeLeft;
        private float _timeToNewTry;

        // Идёт ли игра
        private bool _gameGoing;

        // Коэффициент точности в % 
        private float _accuracy;

        void Start()
        {
            // Создаём ссылки для удобства
            _gestureFolder = FindObjectOfType<GestureFolder>();
            _allDefaultGesture = _gestureFolder.Gesture;

            TimerText.transform.position = new Vector2(Screen.width * 0.9f, Screen.height * 0.9f);
            AccuracyResultImage.gameObject.transform.position = new Vector2(AccuracyResultImage.gameObject.transform.position.x, Screen.height * 0.8f);
        }

        void Update()
        {
            if (_gameGoing)
            {
                // Проверка ПКМ и счётчика _timeToNewTry
                _drawing(Input.GetMouseButton(0) && _timeToNewTry <= 0);

                if (_timeToNewTry > 0)
                {
                    // Обновляем счётчик
                    _timeToNewTry -= Time.deltaTime;
                    // Обновляем статус активности обьекта (ResultLable)
                    AccuracyResultImage.gameObject.SetActive(_timeToNewTry > 0);
                    if (_timeToNewTry <= 0)
                    {
                        if (_accuracy >= OptionAccuracyValue)
                        {
                            _clearAll();
                            _startRound();
                            // Обновляем текст счёт-а
                            ScoreText.text = "You score: " + (_currentRound);
                        }
                        else
                        {
                            // Не прошли проверку на точность, проверяем есть ли времяна повторную попытку
                            if (_timeLeft > 0)
                            {
                                _clearDraw();
                            }
                            else
                            {
                                _clearAll();
                                _showScore();
                            }
                        }
                    }
                }
                else
                {
                    // Обновляем счётчик
                    _timeLeft -= Time.deltaTime * 1000;
                    TimerText.text = (_timeLeft / 1000).ToString("00.00000");
                    if (_timeLeft <= 0)
                    {
                        // Кончилось время - дорисовуем фигуру и проверяем.
                        TimerText.text = "00.00000";
                        _drawing(false);
                        _timeToNewTry = 2;
                    }
                }
            }
        }


        // Изминение значения - слайдера, который меняет параметр расчёта - расстояние между полигонами, ссылка со сцены
        public void ChangeOptionAccuracyResize(Slider slider)
        {
            slider.transform.GetChild(0).GetComponent<Text>().text = slider.value.ToString("0.000");
            OptionAccuracyResize = slider.value;
        }

        // Изминение значения - слайдера, который меняет параметр расчёта - проходной процент точности, ссылка со сцены
        public void ChangeOptionAccuracyValue(Slider slider)
        {
            slider.transform.GetChild(0).GetComponent<Text>().text = slider.value.ToString("0.00");
            OptionAccuracyValue = slider.value;
        }

        // Изминение значения - чекбокса, который меняет параметр - отображение нарисованого в режиме реального времени, ссылка со сцены
        public void ChangeDrawOption()
        {
            WithCursor = !WithCursor;
        }

        // Кнопка старта игры, ссылка со сцены
        public void StartBtnClicked()
        {
            ScoreText.transform.parent.gameObject.SetActive(false);
            StartBtn.gameObject.SetActive(false);
            Options.SetActive(false);
            _newGame();
        }

        // Отобразить счёт
        private void _showScore()
        {
            ScoreText.transform.parent.gameObject.SetActive(true);
            StartBtn.gameObject.SetActive(true);
        }

        // Новая игра
        private void _newGame()
        {
            Debug.Log("NEW GAME");

            // Обнуляем преведущий список очередности фигур, создаём новый - рандомный.
            _gestureFolder.GetureIdList = new List<int>();
            for (int i = 0; i < _gestureFolder.Gesture.Count; i++)
            {
                _gestureFolder.GetureIdList.Add(i);
            }
            System.Random randomForSort = new System.Random();
            _gestureFolder.GetureIdList = _gestureFolder.GetureIdList.OrderBy(v => randomForSort.Next()).ToList();
            // Обнуляем параметры
            TimerText.enabled = true;
            _currentRound = 0;
            ScoreText.text = "You score: " + (_currentRound);
            _startRound();
        }

        // Новый раунд
        private void _startRound()
        {
            // На всякий случай проверяем, остались ли, не пройденные фигуры в списке
            if (_currentRound >= _gestureFolder.GetureIdList.Count)
            {
                _clearAll();
                _showScore();
            }
            Debug.Log("New Round");
            // Определение текущей фигуры - шаблона
            CurrentGestureId = _gestureFolder.GetureIdList[_currentRound];
            // Отображение (построение по точкам) фигуры - шаблона
            _showDefaultGesture(_allDefaultGesture[CurrentGestureId]);
            // Запуск таймера
            _timeLeft = TimeForTimer[_currentRound];
            TimerText.text = (_timeLeft / 1000f).ToString("00.00000");

            // Обнуляем параметры
            _gameGoing = true;
            _accuracy = 0;
            AccuracyResultImage.sprite = AccuracyResultSprites[1];
            AccuracyText.text = "0.00000";
        }

        // Рисование фигуры
        private void _drawing(bool mouseDown)
        {
            if (mouseDown)
            {

                if (_firstMouseCood == new Vector2())
                {
                    // Запоминаем первую точку координат (для замыкания фигуры)
                    _firstMouseCood = Input.mousePosition;
                    _lastMouseCood = Input.mousePosition;
                }
                // Проверка дистанции последней поставленной точкой и текущей, для избежания спама гейм - обьектов
                else if (Vector2.Distance(_lastMouseCood, Input.mousePosition) >= 10)
                {
                    GameObject point;
                    // Построение фигуры происходит соеденением точек (создание линии) координат мыши (преведущей и текущей), 
                    // а так же создаются обекты - точки для сглаживания при изменении угла наклона отрезка, две точки создаются только при построении первого отрезка, дальше только точка текущей координаты мыши, для избежания спама GO
                    if (_drawedGesture.Count == 0)
                    {
                        point = _createPoint(_firstMouseCood, Color.white);
                        point.transform.SetParent(LinesParantTransform);
                        _drawedGO.Add(point);
                        point.SetActive(WithCursor);

                    }
                    point = _createPoint(Input.mousePosition, Color.white);
                    point.transform.SetParent(LinesParantTransform);
                    point.SetActive(WithCursor);
                    _drawedGO.Add(point);

                    GameObject line = _drawLine(_lastMouseCood, Input.mousePosition, Color.white);
                    line.transform.SetParent(LinesParantTransform);
                    _drawedGO.Add(line);
                    line.SetActive(WithCursor);

                    _lastMouseCood = Input.mousePosition;
                }
            }
            else {
                if (_firstMouseCood != new Vector2())
                {
                    if (Vector2.Distance(_lastMouseCood, _firstMouseCood) >= 10)
                    {
                        // создание замыкающей линии
                        GameObject line = _drawLine(_lastMouseCood, _firstMouseCood, Color.white);
                        line.transform.SetParent(LinesParantTransform);
                        _drawedGO.Add(line);
                        line.SetActive(WithCursor);
                    }
                    _lastMouseCood = new Vector2();
                    _firstMouseCood = new Vector2();
                    if (_drawedGO.Count != 0)
                    {
                        _gestureCompare();
                    }
                }

            }
        }

        // Определение процента схожести рисунка с заданным шаблоном
        private void _gestureCompare()
        {

            _resizeAndMoveDraw(_allDefaultGesture[CurrentGestureId]);


            // Если изначально опция WithCursor была выключена, мы показываем фигуру
            if (!WithCursor)
            {
                for (int i = 0; i < _drawedGO.Count; i++)
                {
                    _drawedGO[i].SetActive(true);
                }
            }

            List<Vector2> maxCoods = new List<Vector2>();
            List<Vector2> lowCoods = new List<Vector2>();


            // Создание двух ограничивающих полигона с шагом OptionAccuracyResize
            _defultGestureTransform.localScale = new Vector2(1f + OptionAccuracyResize, 1f + OptionAccuracyResize);
            for (int i = 0; i < _allDefaultGesture[CurrentGestureId].GestureGO.Count; i++)
            {
                maxCoods.Add(_allDefaultGesture[CurrentGestureId].GestureGO[i].transform.position);
            }
            _defultGestureTransform.localScale = new Vector2(1f - OptionAccuracyResize, 1f - OptionAccuracyResize);
            for (int i = 0; i < _allDefaultGesture[CurrentGestureId].GestureGO.Count; i++)
            {
                lowCoods.Add(_allDefaultGesture[CurrentGestureId].GestureGO[i].transform.position);
            }
            _defultGestureTransform.localScale = new Vector2(1f, 1f);



            List<bool> accuracyCheck = new List<bool>();

            // Проходим по всем нарисованым точкам и всем шаблонным точкам, определяем положение точки относительно прямой (из точек шаблона) для двух полигонов.
            for (int a = 0; a < 2; a++)
            {
                List<Vector2> currentCoods = a == 0 ? maxCoods : lowCoods;
                for (int j = 0; j < _drawedGesture.Count; j++)
                {
                    bool compl = a == 0;
                    if (compl)
                    {
                        accuracyCheck.Add(true);
                    }
                    for (int i = 0; i < currentCoods.Count; i++)
                    {
                        float numOut;
                        if (i == (currentCoods.Count - 1))
                        {
                            numOut = (_drawedGesture[j].transform.position.x - currentCoods[i].x) *
                                     (currentCoods[0].y - currentCoods[i].y) -
                                     (_drawedGesture[j].transform.position.y - currentCoods[i].y) *
                                     (currentCoods[0].x - currentCoods[i].x);
                        }
                        else
                        {
                            numOut = (_drawedGesture[j].transform.position.x - currentCoods[i].x) *
                                     (currentCoods[i + 1].y - currentCoods[i].y) -
                                     (_drawedGesture[j].transform.position.y - currentCoods[i].y) *
                                     (currentCoods[i + 1].x - currentCoods[i].x);
                        }
                        if (numOut < 0)
                        {
                            compl = a != 0;
                        }
                    }
                    if (!compl)
                    {
                        accuracyCheck[j] = false;
                    }
                }
            }

            // Вычесление процента точности
            int accuracyOk = 0;
            for (int i = 0; i < accuracyCheck.Count; i++)
            {
                if (accuracyCheck[i])
                {
                    accuracyOk++;
                }
            }
            _accuracy = 100f * accuracyOk / (accuracyCheck.Count);
            // Отображение  процента точности
            AccuracyResultImage.sprite = _accuracy >= OptionAccuracyValue ? AccuracyResultSprites[0] : AccuracyResultSprites[1];
            _timeToNewTry = 2;
            AccuracyResultImage.gameObject.SetActive(true);
            Debug.Log(accuracyOk + " / " + accuracyCheck.Count);
            Debug.Log(_accuracy);
            AccuracyText.text = "Точность: " + _accuracy.ToString("00.00");
        }

        // Изменение размера и перемещение нарисованой фигуры
        private void _resizeAndMoveDraw(GestureModel gestureModel)
        {
            // Вычисляем высоту, ширину и центр фигуры.

            float minX = _drawedGO[0].transform.localPosition.x;
            float minY = _drawedGO[0].transform.localPosition.y;
            float maxX = _drawedGO[0].transform.localPosition.x;
            float maxY = _drawedGO[0].transform.localPosition.y;

            for (int i = 1; i < _drawedGO.Count; i++)
            {
                if (_drawedGO[i].transform.localPosition.x > maxX)
                {
                    maxX = _drawedGO[i].transform.localPosition.x;
                }
                if (_drawedGO[i].transform.localPosition.x < minX)
                {
                    minX = _drawedGO[i].transform.localPosition.x;
                }
                if (_drawedGO[i].transform.localPosition.y > maxY)
                {
                    maxY = _drawedGO[i].transform.localPosition.y;
                }
                if (_drawedGO[i].transform.localPosition.y < minY)
                {
                    minY = _drawedGO[i].transform.localPosition.y;
                }
            }

            Vector2 centralPos = new Vector2(((minX + maxX) / 2f), ((minY + maxY) / 2f));

            // Создание центральной точки
            _centralPoint = new GameObject("Crentral Point");
            _centralPoint.transform.SetParent(LinesParantTransform);
            _centralPoint.transform.localPosition = centralPos;
            _drawedGO.Add(_centralPoint);

            float widthDistance = maxX - minX;
            float heightDistance = maxY - minY;

            var x = gestureModel.WidthDistance / widthDistance;
            var y = gestureModel.HeightDistance / heightDistance;

            // Если рисунок не слишком мал (в 40 раз увеличивается при отрезке в 20-30 (2-3 нарисованых отрезка)) - маштабирауем
            if (((x + y) / 2f) < 40)
            {
                LinesParantTransform.localScale = new Vector2(((x + y) / 2f), ((x + y) / 2f));
            }

            // Высчитываем координаты для перемещение фигуры
            x = _centralPoint.transform.position.x - (gestureModel.CentralPoint.x + Screen.width / 2f);
            y = _centralPoint.transform.position.y - (gestureModel.CentralPoint.y + Screen.height / 2f);

            // Перемешаем фигуру
            LinesParantTransform.position = new Vector2(LinesParantTransform.position.x - x, LinesParantTransform.position.y - y);
        }

        // Создание шаблонной фигуры по заданым точкам
        private void _showDefaultGesture(GestureModel gestureModel)
        {
            GameObject defultGastureParent = new GameObject("Default Gesture");
            defultGastureParent.AddComponent<RectTransform>();
            _defultGestureTransform = defultGastureParent.transform;
            _defultGestureTransform.SetParent(LinesParantTransform.parent);
            _defultGestureTransform.SetSiblingIndex(1);
            GameObject circleFolder = new GameObject("Default Circle Folder");
            _defultCircleFolder = circleFolder.transform;
            _defultCircleFolder.SetParent(_defultGestureTransform);

            for (int i = 0; i < gestureModel.GestureCoods.Count; i++)
            {
                GameObject line = _drawLine(gestureModel.GestureCoods[i], i == gestureModel.GestureCoods.Count - 1 ? gestureModel.GestureCoods[0] : gestureModel.GestureCoods[i + 1], Color.red, false);
                line.transform.SetParent(_defultGestureTransform);
                _gestureGO.Add(line);

                GameObject circle = _createPoint(gestureModel.GestureCoods[i], Color.red);
                circle.transform.SetParent(_defultGestureTransform);
                gestureModel.GestureGO.Add(circle);
                _gestureGO.Add(circle);

            }
            _defultGestureTransform.localPosition = new Vector3();
        }

        // Очистка всей сцены
        private void _clearAll()
        {
            _currentRound++;
            _gameGoing = false;
            for (int i = _gestureGO.Count - 1; i >= 0; i--)
            {
                Destroy(_gestureGO[i]);
            }
            Destroy(_defultGestureTransform.gameObject);

            _clearDraw();
            _allDefaultGesture[CurrentGestureId].GestureGO = new List<GameObject>();
            _gestureGO = new List<GameObject>();
            _defultGestureTransform = null;
        }

        // Очистка сцены от нарисованой фигуры
        private void _clearDraw()
        {
            for (int i = _drawedGO.Count - 1; i >= 0; i--)
            {
                Destroy(_drawedGO[i]);
            }
            Destroy(_centralPoint);
            _drawedGesture = new List<GameObject>();
            _drawedGO = new List<GameObject>();
            _lastMouseCood = new Vector2();
            _firstMouseCood = new Vector2();
            _centralPoint = null;
            AccuracyResultImage.gameObject.SetActive(false);
            LinesParantTransform.localScale = new Vector3(1, 1, 1);
            LinesParantTransform.transform.localPosition = new Vector3();
        }

        // Создание точки, если точка для определение процента схожести, компонент изображения - не добавляем за не надобюностью
        private GameObject _createPoint(Vector2 cood, Color color, bool withoutImg = false)
        {
            GameObject point = new GameObject("point");
            if (!withoutImg)
            {
                Image pointImg = point.AddComponent<Image>();
                pointImg.sprite = CircleSprite;
                pointImg.color = color;
                point.GetComponent<RectTransform>().sizeDelta = new Vector2(11.75f, 11.75f);
            }
            else
            {
                point.AddComponent<RectTransform>().sizeDelta = new Vector2(11.75f, 11.75f);
            }


            point.transform.position = cood;
            return point;
        }

        // Создание линии
        private GameObject _drawLine(Vector2 pointA, Vector2 pointB, Color color, bool addGesture = true)
        {
            GameObject lineGo = new GameObject("Line");
            Image lineImage = lineGo.AddComponent<Image>();
            lineGo.transform.position = (pointA + pointB) / 2f;
            var lengthC = (pointA - pointB).magnitude;
            var sineC = (pointB.y - pointA.y) / lengthC;
            var angleC = Mathf.Asin(sineC) * Mathf.Rad2Deg;
            if (pointB.x < pointA.x)
                angleC = 0 - angleC;
            lineGo.transform.rotation = Quaternion.Euler(0, 0, angleC);
            lineGo.GetComponent<RectTransform>().sizeDelta = new Vector2(Vector3.Distance(pointA, pointB) + 3, 10);
            lineImage.color = color;

            if (addGesture)
            {
                // Если расстояние между точками слишком велико, создаём много точек для повышения точности- определение процента схожести
                do
                {
                    pointA = Vector2.MoveTowards(pointA, pointB, 20);
                    var point = _createPoint(pointA, Color.white, true);
                    point.transform.SetParent(PointParentTransform);
                    point.SetActive(WithCursor);
                    _drawedGesture.Add(point);
                    _drawedGO.Add(point);

                } while (Vector2.Distance(pointA, pointB) > 20);
            }
            return lineGo;
        }
    }
}
