using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class GestureFolder : MonoBehaviour
    {

        public List<GestureModel> Gesture = new List<GestureModel>();

        // Список айдишников для листа Gesture
        public List<int> GetureIdList = new List<int>();

        void Start()
        {
            
            ReadFile("ShapeCoods.txt");
        }

        void Update()
        {

        }
        public void ReadFile(string filename)
        {
            // Если файла нет, выходим из приложения т.к. играт ьневозможно без шаблонных фигур
            if (!File.Exists(filename))
            {
                Debug.Log("NO FILE WITH COODS, NAME "+ filename);
                Application.Quit();
            }

            // Открываем файл, спилитим по переходу на новую строку (\n) и пробуем сериализируем, если ошибка (например пустая строка), пропускаем строку
            var sr = File.OpenText(filename);
            var fileLines = sr.ReadToEnd().Split('\n').ToList();
            for (int i = 0; i < fileLines.Count; i++)
            {
                try
                {
                    var myObject = JsonUtility.FromJson<GestureModel>(fileLines[i]);
                    Gesture.Add(myObject);
                    _setOptions(myObject);
                }
                catch (Exception err)
                {
                    Debug.Log("ERROR! serialize :" + err);
                    continue;
                }

            }
            sr.Close();
        }

        // Вычисляем высоту, ширину и центр фигуры.
        private void _setOptions(GestureModel gestureModel)
        {

          
            float minX = gestureModel.GestureCoods[0].x;
            float minY = gestureModel.GestureCoods[0].y;
            float maxX = gestureModel.GestureCoods[0].x;
            float maxY = gestureModel.GestureCoods[0].y;

            for (int i = 1; i < gestureModel.GestureCoods.Count; i++)
            {
                if (gestureModel.GestureCoods[i].x > maxX)
                {
                    maxX = gestureModel.GestureCoods[i].x;
                }
                if (gestureModel.GestureCoods[i].x < minX)
                {
                    minX = gestureModel.GestureCoods[i].x;
                }
                if (gestureModel.GestureCoods[i].y > maxY)
                {
                    maxY = gestureModel.GestureCoods[i].y;
                }
                if (gestureModel.GestureCoods[i].y < minY)
                {
                    minY = gestureModel.GestureCoods[i].y;
                }
            }
            gestureModel.CentralPoint = new Vector2(((minX + maxX) / 2f), ((minY + maxY) / 2f));

            if (minX < 0)
            {
                gestureModel.WidthDistance = maxX + (minX * -1);
            }
            else
            {
                gestureModel.WidthDistance = maxX - minX;
            }

            if (minY < 0)
            {
                gestureModel.HeightDistance = maxY + (minY * -1);
            }
            else
            {
                gestureModel.HeightDistance = maxY - minY;
            }

        }
    }


    // Модель для шаблонных фигур
    [Serializable]
    public class GestureModel
    {
        public List<Vector2> GestureCoods;
        public float WidthDistance;
        public float HeightDistance;
        public Vector2 CentralPoint;
        public List<GameObject> GestureGO = new List<GameObject>();
    }
}