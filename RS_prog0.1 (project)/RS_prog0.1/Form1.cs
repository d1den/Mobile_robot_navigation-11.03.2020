/* 
NAME: Денисов Дмитрий Сергеевич, КРБО-01-17
SUBJ: Системы навигации автономных роботов
TITLE: Система моделирования робота в думерном дискретном мире
DATE: 11.03.2020

"Discrete World Robot Modeling System" 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace RS_prog0._1
{
    public partial class Form1 : Form
    {
        // Объявление глобальных переменных и массивов
        static public int yMax = 0, xMax = 0; // Размеры полей
        static public Button[,] btnMatrix = null; // Объявляем 2д матрицу кнопок
        static public double[,] p = null; // Матрица вероятностей расположения робота
        static public string[,] world = null; // Массив строк, хранящий цвета созданного мира
        static public int yRealPrev = 0; // Предыдущее значение реальной позиции по y
        static public int xRealPrev = 0; // Предыдущее значение реальной позиции по y
        static public int yReal = 0; // Реальная позиция робота по Y и X
        static public int xReal = 0;

        static public int countBugSense = 0; // Счётчик считывания неправильного цвета
        static public int countSense = 0; // Счётчик считывания
        static public int countBugMoveX = 0; // Счётчик ошибок движения по X
        static public int countBugMoveY = 0; // Счётчик ошибок движения по Y
        static public int countMove = 0; // Счётчик движения

        public volatile bool flagRealMotion = false; // Флаг выполнения функции движения робота
        public bool flagButton2Click = false; // Флаг нажатия второй кнопки, означающий, что начальные позиции выбраны

        static double pHit = 0.6; // Вероятность правильного считывания цвета датчиком
        static double pMiss = 0.2; // Вероятность неправильного считывания
        static double pExact = 0.8; // Вероятность, что робот проехал нужное расстояние
        static double pOvershoot = 0.1; // Вероятность, что робот перехал
        static double pUndershoot = 0.1; // Вероятность, что робот не доехал
        

        List <string> start_position = new List <String>(); // Список, для записи стартовых позиций

        public Form1()
        {
            InitializeComponent();
        }
        
        // Функция загрузки первой формы. Выводим приветствующий текст на экран
        private void Form1_Load(object sender, EventArgs e)
        {
            textBoxMain.Text = " Здравствуйте! Для начала работы выберите размер требуемого поля и нжамите кнопку 'Создать поле'.";
            textBoxX.Text = "0"; // Заполлняем начальные значения текстбоксов
            textBoxY.Text = "0";
        }
        
        // Функция нажатия на 1 кнопку (создать матрицу)
        private void button1_Click(object sender, EventArgs e)
        {
            flagButton2Click = false; // Сбрасывем фалг нажатия второй кнопки
            xReal = xRealPrev = yReal = yRealPrev = 0; // Сбрасываем позиции реального робота
            start_position.Clear(); // Очищаем список стартовых позиций
            panel1.Controls.Clear(); // Очищаем панель
            if (rb1.Checked) yMax = xMax = 3; // Задаём масштабы сетки в соотвествии с нажатой radio_button
            else if (rb2.Checked) yMax = xMax = 5;
            else if (rb3.Checked) yMax = xMax = 7;

            btnMatrix = new Button[yMax, xMax]; // Инициализируем матрицу кнопок нужного нам размера
            p = new double[yMax, xMax]; // Инициализируем матрицу вероятностей 
            world = new string[yMax, xMax]; // Инициализируем матрицу мира
            for (int y=0;y<yMax;y++)
            {
                for (int x=0;x<xMax;x++)
                {
                    btnMatrix[y, x] = new Button(); // Инициализируем каждую кнопку
                    btnMatrix[y, x].Height = btnMatrix[y, x].Width = 50; // Размеры кнопки
                    btnMatrix[y, x].Left = panel1.Left + x * 50;// Расположение кнопки
                    btnMatrix[y, x].Top = panel1.Top + y * 50;
                    btnMatrix[y, x].Tag = String.Format("{0},{1}", y, x); // В Тэг кнопки записываем её координаты
                    btnMatrix[y, x].Click += btn_Click; // Прописание клика кнопки
                    btnMatrix[y, x].Font = new Font("Microsoft Sans Serif",9); // Выставляем изначально размер шрифта 9
                    panel1.Controls.Add(btnMatrix[y, x]); // Добавление кнопки в панель
                }
            }
            world_generate(); // Вызываем функцию, создания мира (заполнение сетки цветами)
            // Выводим текст ,предлагающий следующее действие пользователю
            textBoxMain.Text = "Теперь выберите ячейки, где возможно расположение робота (либо не выбирайте). Затем нажмите 'Готово'.";
        }

        // Функция обработки нажатия кнопок массива
        private void btn_Click(object sender, EventArgs e) 
        {
            if (!flagButton2Click) // Проверяем, можно ли вводить стартовые позиции (не нажата вторая кнопка)
            {
                Button b = sender as Button;
                string index = Convert.ToString(b.Tag); // Конвертируем в строку Тэг кнопки
                start_position.Add(index); // Заносим индекс нажатой кнопки в список стартовых позиций 
                int position = index.IndexOf(","); // Находим индекс разделителя (,)
                int y = Convert.ToInt32(index.Substring(0, position)); // Выделяем Y
                int x = Convert.ToInt32(index.Substring(position + 1)); // Выделяем X
                btnMatrix[y, x].Text = "Press"; // Выводим на нажатую кнопку, что она нажата 
            }
        }
        // Функция нажатия второй кнопки ("Готов" - начальные позиции заданы)
        private void button2_Click(object sender, EventArgs e)
        {
            if (!flagButton2Click)
            {
                // Выводим текст ,предлагающий следующее действие пользователю
                textBoxMain.Text = String.Format("Теперь можете задать желаемое перемещение по осям X и Y:\n (!|dX|<{0}, |dY|<{1}!), после чего нажмите 'Ввести', затем будет произведён расчёт вероятностей и перемещение робота.",xMax, yMax);
                int countStart = start_position.Count(); // Выполняем подчёт количества стартовых позиций
                if (countStart == 0) // Если не выбрано не одной стартовой позиции, то получается равновероятное распределение
                {
                    for (int i = 0; i < yMax; i++)
                        for (int j = 0; j < xMax; j++)
                            p[i, j] = (double)1 / (yMax * xMax); // Заполняем все клетки матрицы равными значениями
                    // Реального робота располагаем в случайном месте сетки
                    Random rnd = new Random(); // Создаём объект рандом
                    yReal = rnd.Next(yMax); // Выбираем случайную координату по X и по Y для реального робота
                    xReal = rnd.Next(xMax);
                }
                else // Если выбрана хоть одна стартовая позиция
                {
                    int[] yForRnd = new int[countStart]; // Создаём массивы, куда запишем все координаты выбранных стартовых позиций
                    int[] xForRnd = new int[countStart];
                    for (int i = 0; i < countStart; i++)
                    {
                        string index = start_position[i]; // Считываем индекс стартовой позиции
                        int position = index.IndexOf(","); // Находим индекс разделителя (,)
                        int y = Convert.ToInt32(index.Substring(0, position)); // Выделяем Y
                        yForRnd[i] = y; // Заносим Y в массив координат Y
                        int x = Convert.ToInt32(index.Substring(position + 1)); // Выделяем X
                        xForRnd[i] = x; // Заносим X в массив координат X
                        p[y, x] = (double)1 / countStart; // Считаем вероятность одинаковую вероятность для заданных позиций
                    }
                    // Случайно выбираем реальное расположение робота в одной из стартовых позиций
                    Random rnd = new Random();
                    int xyIndex = rnd.Next(countStart);
                    yReal = yForRnd[xyIndex];
                    xReal = xForRnd[xyIndex];
                }
                p_print(); // Вызываем функцию вывода вероятности на кнопки
                flagButton2Click = true; // Устанавливаем флаг нажатия второй кнопки
            }
        }

        // Функция нажатия 3ей кнопки ("Ввести" - используя введённые значения расчитать и выести пермещение реальног робота и вероятность)
        private void button3_Click(object sender, EventArgs e)
        {
            if (flagButton2Click)
            {
                int[] motions = new int[2]; // Массив, хранящий пермещения по X и Y
                try // В помощью try отлавливаем ошибки ввода значений (введена строка, либо нецелочисленное число)
                {
                    motions[0] = int.Parse(textBoxX.Text); // В первой ячейке X, заносится из текстбокса
                    motions[1] = -int.Parse(textBoxY.Text); // Во второй Y. Минус для того, чтобы при вводе положительного значения, совершалось движение вверх

                    if (Math.Abs(motions[0]) < xMax && Math.Abs(motions[1]) < yMax) // Проверяем правильность введённых данных
                    {
                        string measurements = realSense(); // Считываем значение цвета под собой
                        p = sense(p, measurements); // Расчитываем веротяность в зависимости от цвета под собой
                        p = move(p, motions); // Рассчитываем веротяность при движении
                        p_print(); // Выводим веротятность на экран
                        /* Для крассивой дискретной отрисовки движения робота используется метод realMotion,
                         * в котором каждый шаг робота отрисовывается с небольшой задержкой (300 мс), которая выполнена через
                         * Thread.Sleep. Для того, чтобы тормозилась не вся программа, данный метод далее выделяется отдельной задачей.
                         * Однако, из-за этого могут случаться вылеты программы. Для их устранения были приняты меры, 
                         * но при необходимости четкой работы программы - раскомментировать следующую строку и закомментировать вызов задачи.
                         */
                        //realMotion(motions);
                        if (!flagRealMotion) // Если данная задача уже не запущена
                        Task.Run(() => realMotion(motions)); // Вызов функции реального перемещения робота в отдельной задаче
                    }
                    else
                    {
                        // Вывзывается Окно сообщения об ошибке, где сообщается, на что именно обратить внимание
                        MessageBox.Show(
                            String.Format("Были введены некорректные данные. Проверьте, чтобы перемещения были по модулю меньше размера сетки:\n|dX| < {0}, |dY| < {1}!", xMax, yMax),
                            "Ошибка ввода!",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.DefaultDesktopOnly);
                    }
                }
                catch
                {
                    // Вывзывается Окно сообщения об ошибке, где сообщается, на что именно обратить внимание
                    MessageBox.Show(
                            "Были введены некорректные данные. Проверьте, введены ли значения пермещений по X и по Y, и являются ли они целым числом!",
                            "Ошибка ввода!",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }
        // Функция вывода вероятности на кнопки
        private void p_print()
        {
            btnMatrix[yRealPrev, xRealPrev].Image = null; // Сбрасываем изображение в предыдущем месте реального робота
            List<double> pLineCopy = new List<double>(); // Создаём список для хранения строк массива вероятностей
            double pMax = 0; // Переменная для хранения значения максимальной вероятности
            for (int i = 0; i < p.GetLength(0);i++)
            {
                pLineCopy.Clear(); // Очищаем список, содержащей элементы строки массива вероятностей
                for (int j = 0; j < p.GetLength(1); j++)
                {
                    btnMatrix[i, j].Font = new Font("Microsoft Sans Serif", 9); // Выставляем всем кнопкам размер шрифта 9
                    pLineCopy.Add(p[i, j]); // Добавляем элементы строки P в список
                }
                pLineCopy.Sort(); // Сортируем список для нахождения наибольшей вероятности
                if (pLineCopy[p.GetLength(1)-1] > pMax) // Если наибольшее значение больше pMax, то оно становится pMax
                    pMax = pLineCopy[p.GetLength(1) - 1];
            }
                for (int i = 0; i < p.GetLength(0); i++)
                    for (int j = 0; j < p.GetLength(1); j++)
                    {
                        if(p[i,j] == pMax) // Если в данной ячейке находится наибольшая вероятность, то
                            btnMatrix[i, j].Font = new Font("Microsoft Sans Serif", 12); // Делаем увеличенный шрифт
                        btnMatrix[i, j].Text = String.Format("{0:F2}", p[i, j]); // Выводим вероятность текстом на кнопку
                        // Выводим изображение на кнопку
                        btnMatrix[yReal, xReal].Image = Image.FromFile("1.jpg");
                        yRealPrev = yReal; // Заносим данные координаты, как предыдущие
                        xRealPrev = xReal;
                    }
        }
        // Функция генерации мира цветов
        private void world_generate()
        {
            string[] colors = { "Red", "Green", "Blue", "Yellow" }; // Задам массив с доступными цветами
            Random rnd = new Random(); // Создаём объект рандом
            for (int i = 0; i < yMax; i++)
            {
                for (int j = 0; j < xMax; j++)
                {
                    int colorIndex = rnd.Next(colors.Length); // Определяем случайный индекс цвета в массиве цветов
                    world[i, j] = colors[colorIndex]; // Заполняем цветами наш мир
                    btnMatrix[i, j].BackColor = Color.FromName(world[i, j]); // Выводим цвет на кнопку
                }
            }
        }

        private string realSense() // Функция считывания датчиком цвета поля
        {
            // Данная функция прописывает алгоритм, что из 10 считываний 3 могут быть ложными (а могут и не быть)
            string[] colors = { "Red", "Green", "Blue", "Yellow" }; // Массив возможных цветов
            string colorSense = null; // Строка, содержащая реальный цвет яцейки, в которой находится робот
            string sense; // Строка, которая будет содержать значение цвета, полученное датчиком
            colorSense = world[yReal,xReal]; // Записываем реальный цвет ячейки, в которой находится робот

            if (countBugSense < 3) // Если количество ошибок меньше 3
            {
                Random rnd = new Random(); // Создаём рандомный объект
                int colorId = rnd.Next(colors.Length); // Получаем случайное число - индекс цвета в массиве цветов
                sense = colors[colorId]; // Получаем случайный цвет
                if (sense != colorSense) // Если полученный цвет не тот цвет, который на самом деле находится в ячейке,
                    countBugSense++; // то увеличиваем счётчик ошибки
                countSense++; // Увеличиваем счётчик считываний
            }
            else // Если ошибок больше 3-х
                sense = colorSense; // То считанный цвет равен реальному
            if (countSense == 9) // Если прошло 10 измерений, то сбрасываем значений счётчиков
            {
                countSense = countBugSense = 0;
            }
            return sense; // Возвращаем из функции считанный цвет

        }

        // Функция расчёта веротяности в зависимости от полученного значения с датчика
        static double[,] sense(double[,] p, string Z) 
        {
            double[,] q = new double[p.GetLength(0), p.GetLength(1)]; // Создаём дополнительный массив
            double s = 0; // И переменную, для нормирования вероятности
            for (int i = 0; i < p.GetLength(0); i++) // Рассчитываем веротяность в зависимости от определённого цвета
            {
                for (int j = 0; j < p.GetLength(1); j++)
                {
                    int hit = (Z == world[i, j]) ? 1 : 0; // Проверяем, правильно ли был определён цвет
                    q[i, j] = p[i, j] * (pHit * hit + pMiss * (1 - hit)); // Считаем условную веротяность
                    s += q[i, j]; // Складываем все значения в переменную для нормированния
                }
            }
            for (int i = 0; i < p.GetLength(0); i++) // Приводим вероятность к нормальному виду
            {
                for (int j = 0; j < p.GetLength(1); j++)
                {
                    q[i, j] /= s; // Нормируем веротяность
                }
            }
            return q;
        }

        // Функция расчёта вероятностей при движении
        static double[,] move(double[,] p, int[] U)
        {
            double[,] q = new double[p.GetLength(0), p.GetLength(1)]; // Дополнительный массив
            for (int i = 0; i < p.GetLength(0); i++) // Расччитываем вероятность для каждой точки сначала при движении по X
            {
                for (int j = 0; j < p.GetLength(1); j++)
                {
                    double t = p[i, (p.GetLength(1) + j - U[0]) % p.GetLength(1)] * pExact; // Рассчитываем вероятность для каждой точки, что доехали
                    t += p[i, (p.GetLength(1) + j - U[0] - 1) % p.GetLength(1)] * pUndershoot; // Не доехали
                    t += p[i, (p.GetLength(1) + j - U[0] + 1) % p.GetLength(1)] * pOvershoot; // Перехали
                    q[i, j] = t;
                }
            }

            double[,] r = new double[p.GetLength(0), p.GetLength(1)]; // Дополнительный массив

            for (int i = 0; i < q.GetLength(0); i++) // Теперь считаем веротяность для каждой точки при движении по Y
            {
                for (int j = 0; j < q.GetLength(1); j++)
                {
                    double t = q[(q.GetLength(0) + i - U[1]) % q.GetLength(0), j] * pExact; // Доехали
                    t += q[(q.GetLength(0) + i - U[1] - 1) % q.GetLength(0), j] * pUndershoot; // Не доехали
                    t += q[(q.GetLength(0) + i - U[1] + 1) % q.GetLength(0), j] * pOvershoot; // Перехали
                    r[i, j] = t;
                }
            }
            return r;
        }

        // Функция движения реального робота
        private void realMotion(int[] U)
        {
            flagRealMotion = true; // Устанавливаем флаг, что началось выполнение функции
            int helpX = 0; // Переменные для дискретного пермещения робота (по 1-му шагу)
            int helpY = 0;
            if (U[0] > 0) helpX = 1; // Задаём их 1 или -1, в заисимости от знака
            else helpX = -1;
            if (U[1] > 0) helpY = 1;
            else helpY = -1;
            Random rnd = new Random(); // Создаём объект рандом, чтобы случайно создавать ошибку - не доехал или переехал робот
            int[] bugU = { -1, 1, 0 }; // Массив, хранящий значения - не доехал, переехал или доехал как надо
            int dX = Math.Abs(U[0]); // Берём модуль нашего от суммарного перемещения по X и по Y
            int dY = Math.Abs(U[1]);
            if (countBugMoveX < 1) // Мы допускаем из 10 движений робота 1 ошибку по X и одну по Y
            {
                int bugX = bugU[rnd.Next(bugU.Length)]; // Ошибка по X - выбирается случайно из массива возможных ошибок
                if (bugX != 0)
                    countBugMoveX++; // Если робот не доехал или перехал, то прибавляем увеличиваем счётчик ошибки по X
                dX += bugX;
            }
            if (countBugMoveX < 1) // Аналогино допускается одна ошибка по Y
            {
                int bugY = bugU[rnd.Next(bugU.Length)];
                if (bugY != 0)
                    countBugMoveY++;
                dY += bugY;
            }
            countMove++; // Увеличиваем счётчик движения
            for (int i=0;i<dX;i++)
            {
                xRealPrev = xReal; // Задаём предудщее значение реального робота
                xReal = (xMax + xReal + helpX) % xMax; // Задаём по выведенной формуле пермещение реальнго робота
            labelX:
                try // Пытаемся отловить ошибку, вызванную выделением данного метода как отдельной задачи, для отрисовки робота
                {
                    p_print(); // Вызываем функцию отрисовки робота и веротяности
                }
                catch // Если ошибка, то переходим на метку и снова пробуем выполнить отрисовку
                {
                    goto labelX;
                }
                Thread.Sleep(300); // Выполняем задержку 300 мс
            }
            // С Y пермещением происходят аналогичные вычисления
            for (int i = 0; i < dY; i++)
            {
                yRealPrev = yReal;
                yReal = (yMax + yReal + helpY) % yMax;
            labelY:
                try
                {
                    p_print();
                }
                catch
                {
                    goto labelY;
                }
                Thread.Sleep(300);
            }
            if (countMove == 9) // Если было совершено 10 пермещений, то сбрасываем счётчики
                countBugMoveX = countBugMoveY = countMove = 0;
            flagRealMotion = false; // Сбрасываем флаг выполнение данного метода
        }

    }
}
