# PeliCode

Juego en Unity para practicar programación en C#. El jugador inicia sesión, elige una categoría y una dificultad, y responde preguntas de cuatro tipos con tiempo. En el modo ranked las preguntas se mezclan y la partida sigue hasta el primer error. El mejor puntaje entra al Top 25.

**Dificultad en ranked:** sube con los aciertos. `RankedQuestionPicker` elige la dificultad de cada pregunta según cuántas lleva bien el jugador, y las pondera así (se interpola de forma lineal):

| Aciertos | Fácil | Medio | Difícil |
| --- | --- | --- | --- |
| 0 | 70 % | 25 % | 5 % |
| 10 | 40 % | 30 % | 30 % |
| 20 o más | 10 % | 35 % | 55 % |

Ninguna dificultad llega a cero, así que siempre puede salir una fácil o una difícil. Dentro de una dificultad no se repite una pregunta hasta haber visto todas las demás. Los números están en `RankedQuestionPicker.cs` (`Start`, `End` y `RampLength`).

El juego está en español y en inglés.

## Requisitos

- Unity **6000.3.19f1** (URP 2D).
- Un proyecto de Unity Gaming Services vinculado (Project Settings > Services) con:
  - **Authentication**, con el proveedor *Username & Password* activado.
  - **Cloud Save**, para el perfil del jugador, su historial de partidas, las preguntas y los anuncios.
  - **Leaderboards**, con un leaderboard de ID `ranked_top_score` (orden descendente, *Keep best*).

## Escenas

En `Assets/Scenes`, en este orden en Build Settings:

1. **Login**: inicio de sesión y registro, con el nombre del juego arriba a la izquierda (`Text_Title`: "Peli" en #5499CF y "Code#" en #CBDDEF). Contiene el GameObject `Managers` con los managers que persisten entre escenas.
2. **Menu**: cinco paneles que se deslizan (Anuncios, Perfil, Minijuegos, Ranked y Opciones).
3. **MiniGame**: la partida.

Todos los cambios de escena pasan por `SceneLoader`, que usa `SceneTransition`:

1. La escena actual se funde a negro.
2. Aparece la pantalla de carga: "CARGANDO" con los puntos de 1 a 3 y el logo (`Assets/Resources/PeliCode_Logo.png`), que late mientras carga.
3. La pantalla de carga se mantiene hasta que la escena nueva está lista. Una escena que necesita preparar algo implementa `ISceneReadiness`; el minijuego, por ejemplo, espera a tener las preguntas.
4. Se funde a negro y aparece la escena nueva.

La pantalla de carga se construye por código, así que no hay que agregar nada a las escenas.

**Pantallas:** el diseño es de 1920x1080. Los `CanvasScaler` están en modo *Expand*, así que en pantallas 16:10, 4:3, 5:4 o ultrawide todo entra entero y lo que sobra queda como fondo.

**Sesión:** al iniciar sesión, Unity guarda la sesión en el dispositivo. La próxima vez el juego entra directo al menú, hasta que el jugador cierre sesión en Opciones. Mientras el login comprueba si hay una sesión guardada, la pantalla de carga sigue a la vista: no se ve el login un instante antes de pasar al menú (`LoginUIController` implementa `ISceneReadiness`).

**Contraseña:** el login y la ventana de crear cuenta traen un botón *Mostrar / Ocultar* dentro del campo (`PasswordToggle`, se arma por código con `PasswordToggle.Attach(campo)`). En Opciones, *Cambiar contraseña* abre una ventana (`ChangePasswordPopup`, también por código) con la contraseña actual, la nueva y su repetición; usa `UpdatePasswordAsync` de Unity y la misma regla de contraseña que al crear la cuenta. Es opcional: solo la usa quien quiere cambiarla. **Recuperar una contraseña olvidada todavía no existe**: Unity Authentication con usuario y contraseña no envía correos, y aquí el correo se guarda como un hash que sirve de nombre de usuario. Hace falta decidir entre pasar a Unity Player Accounts o guardar el correo aparte para enviar un código.

**Icono:** el logo también es el icono del juego (Project Settings > Player > Default Icon).

**Rachas y semanas:** se cuentan con la fecha y hora del dispositivo del jugador, así el día cambia a medianoche en su zona horaria. Los porcentajes de los minijuegos se guardan en el perfil (Cloud Save) y se reinician cada fin de semana: la semana va de **sábado 00:00 a viernes**, y el día de inicio es `ProfileRules.WeekStartsOn`. El porcentaje **combina todas las partidas de la semana** de esa categoría y dificultad: aciertos sobre preguntas respondidas. Por ejemplo, una partida al 100 % y otra al 0 % dan 50 %. En Perfil, la racha muestra su número con cualquier valor mayor que 0 y la imagen solo desde 5 días.

**Colores:** salen de cuatro tonos, definidos en `UITheme` (`Dark` #00070D, `Mid` #032D50, `Light` #5499CF y `White` #CBDDEF). Si se cambian, hay que actualizar también los colores guardados en las escenas y prefabs.

**Al fallar una pregunta:** en selección múltiple y en detectar el error, la elegida queda en rojo y la correcta en verde. En completar el código, lo escrito queda en rojo y la respuesta correcta aparece en verde dentro del código. En ordenar el código, los bloques bien puestos quedan en verde y los demás en rojo; a los 2 segundos se acomodan solos y el jugador tiene unos 4,5 segundos más para estudiarlos.

## Estructura del código (`Assets/Scripts`)

| Carpeta | Qué hay |
| --- | --- |
| `Authentication` | Login y registro con Unity Authentication. El correo se convierte en un hash que sirve de nombre de usuario, y la validación de contraseña es la misma que exige Unity. |
| `CloudServices` | Cloud Save (perfil y preguntas) y Leaderboards. |
| `Core` | `GameManager`, inicio de Unity Services, cambio de escenas y el `Singleton` base. |
| `Data` | Modelos (`PlayerProfileData`, `QuestionData`) y reglas puras: rachas, reseteo semanal y validación de preguntas. |
| `Localization` | Sistema de idiomas: `Loc`, `LocalizedText` y el botón de idioma. |
| `Menu` | Paneles del menú. |
| `MiniGame` | `MiniGameManager`, cronómetro, puntaje y las cuatro vistas de pregunta. |
| `UI` | Paleta (`UITheme`), formato de código y navegación con teclado en formularios. |
| `Audio` | Efectos de sonido (generados por código) y volúmenes. |

## Idiomas

Los textos de la interfaz están en `Assets/Resources/Localization/Textos.txt`, con una fila por texto: `clave`, `es` y `en`, separados por **tabuladores**. Se puede editar en una hoja de cálculo.

- Un texto fijo de una escena lleva el componente `LocalizedText` con su clave.
- Un texto armado desde código usa `Loc.Get("clave")` o `Loc.Format("clave", valores)`.
- El idioma se cambia con el botón de Login o en Opciones, y queda guardado. La primera vez se usa el del sistema.

Las pruebas avisan si falta una traducción o si una escena o el código usan una clave que no está en la tabla.

## Preguntas

Cada categoría y dificultad es una entrada de **Cloud Save > Custom Items**:

- **Custom ID** y **clave**: `questions_{Categoria}_{Dificultad}`, por ejemplo `questions_Condicionales_Facil`.
- **Valor**: un JSON `{"questions": [...]}`.

Categorías: `VariablesTiposOperadores`, `Condicionales`, `SwitchEstados`, `ColeccionesArraysListas` y `BucleWhileDoWhile`. Dificultades: `Facil`, `Medio` y `Dificil`.

Campos de cada pregunta (`QuestionData`):

| Tipo (`type`) | Campos |
| --- | --- |
| `SeleccionMultiple` | `correctAnswer` y `incorrectAnswerPool` (lo ideal son 8 incorrectas; se muestran 3 al azar). |
| `CompletarCodigo` | `codeTemplate`, con `____` donde va la respuesta, y `expectedAnswer`. La comparación **distingue mayúsculas**, como C# (`Int` no es `int`), e ignora los espacios de más. |
| `ArrastrarCodigo` | `codeBlocks` y `correctOrder` (índices de `codeBlocks` en orden). **Máximo 8 bloques**, porque caben 2 filas de 4; una pregunta con más se descarta. No conviene repetir líneas idénticas, porque el orden queda ambiguo. |
| `DetectarError` | `codeSnippetsOptions` y `buggyIndex`. **Máximo 5 fragmentos**, porque la pantalla tiene 5 botones; una pregunta con más se descarta. |

Todas llevan `id`, `type` y `questionText`. Una pregunta mal cargada se descarta con un aviso en la consola, en vez de colgar la partida.

**Medidas de los textos**, para que no se descuadren (`QuestionChecks`, y la prueba `EveryBankQuestionFitsItsTextLimits` revisa el banco local):

| Tipo | Enunciado (`questionText` y `questionTextEn`) | Otros |
| --- | --- | --- |
| `SeleccionMultiple` | 2 filas de 73 caracteres | |
| `CompletarCodigo` | 2 filas de 83 caracteres | `codeTemplate`: 11 filas de 94 caracteres |
| `ArrastrarCodigo` | 2 filas de 83 caracteres | |
| `DetectarError` | 2 filas de 83 caracteres | |

Las palabras que no caben pasan a la fila siguiente y cada salto de línea (`
`) empieza una fila. En selección múltiple, el código de la pregunta va en la misma línea, después de la pregunta y separado por dos espacios: `¿Qué muestra?  int a = 8;  Console.WriteLine(a);`.

Si el jugador se equivoca, el juego muestra la respuesta correcta antes de seguir. Opcionalmente, la pregunta puede traer `explanation`, con su versión en inglés `explanationEn`: un texto corto que explica por qué la respuesta es esa y que aparece debajo del "Incorrecto".

### Preguntas en inglés

El texto de cada pregunta viene del Dashboard, así que la traducción también va ahí, en la misma pregunta. Si a una pregunta le falta la versión en inglés, el juego en inglés la muestra en español.

- `questionTextEn`: el enunciado en inglés. Sirve para todos los tipos.
- `explanationEn`: la explicación en inglés, si la pregunta trae `explanation`.
- `correctAnswerEn` e `incorrectAnswerPoolEn`: para selección múltiple, cuando las respuestas son texto y no código. Van juntos; si falta uno, se usan los dos en español.
- El código (`codeTemplate`, `expectedAnswer`, `codeBlocks` y `codeSnippetsOptions`) no se traduce: es el mismo en los dos idiomas.

Ejemplo:

```json
{
  "id": "loop-medio-7",
  "type": "SeleccionMultiple",
  "questionText": "¿Qué hace continue dentro de un while?",
  "questionTextEn": "What does continue do inside a while loop?",
  "correctAnswer": "Salta a la siguiente vuelta",
  "incorrectAnswerPool": ["Termina el bucle", "Repite la vuelta actual", "Termina el programa"],
  "correctAnswerEn": "Skips to the next iteration",
  "incorrectAnswerPoolEn": ["Ends the loop", "Repeats the current iteration", "Ends the program"]
}
```

Los 15 archivos de `Assets/Resources/Questions` ya traen los dos idiomas y sirven de modelo.

### Banco local

`Assets/Resources/Questions/` trae el banco local: 1449 preguntas, entre 80 y 104 por categoría y dificultad, en los dos idiomas. Cada partida normal toma 5 en Fácil, 10 en Normal y 15 en Difícil. Las respuestas se comprobaron con el compilador de C#: las salidas, los fragmentos que no compilan y los que fallan al ejecutarse. Los archivos tienen el mismo formato que el valor en Cloud Save, así que sirven para copiarlos al Dashboard. El juego los usa cuando Cloud Save falla o no tiene preguntas para esa categoría y dificultad. Si Cloud Save tiene preguntas, esas tienen prioridad.

`PreguntasRetiradas/`, fuera de `Assets`, guarda con el mismo formato las 84 preguntas de selección múltiple que no caben en 2 filas de 73 caracteres (su código ocupa varias líneas). No están en el juego; se pueden volver a usar si la vista de selección múltiple tiene un recuadro para el código, como la de completar.

Para probar el minijuego sin cuenta ni conexión, abre la escena **MiniGame** en el editor y dale Play: usa el banco local y no guarda nada.

## Datos del jugador (Cloud Save > Player Data)

El juego guarda dos claves por jugador. Se crean solas la primera vez que se guardan: **no hay que crear nada en el Dashboard**.

- `player_profile`: nombre, correo, rachas, mejor puntaje ranked y `minigameStats`. Por categoría y dificultad trae el porcentaje de la semana (`easyPercentage`, `mediumPercentage`, `hardPercentage`) y lo que lo forma: partidas (`easyGames`…), aciertos (`easyCorrect`…) y preguntas respondidas (`easyAnswered`…). Se reinicia los sábados.
- `game_history`: la lista de partidas jugadas (`GameRecord`), que **no se reinicia**. Cada una trae `date` (fecha y hora del jugador, con su zona), `mode` (`normal` o `ranked`), `category`, `difficulty`, `correct`, `answered`, `questions`, `seconds` y `completed` (`false` si salió a mitad de partida; se guarda solo si ya respondió alguna pregunta). Una partida abandonada no cambia los porcentajes ni el ranking.

Para verlos: Unity Dashboard > el proyecto > **Player Management**, buscar al jugador por su Player ID y abrir su **Cloud Save**.

## Anuncios

Es una entrada de Cloud Save > Custom Items con Custom ID y clave `announcements`. El valor puede ser un texto, que se muestra igual en los dos idiomas, o un objeto `{"es": "...", "en": "..."}`.

## Pruebas

Hay pruebas EditMode en `Assets/Tests/Editor`: validación, rachas, porcentaje acumulado, historial de partidas, opciones de selección múltiple, arrastrar bloques, idiomas, banco de preguntas (incluidas las medidas de los textos), logo e icono, y una partida corta con el banco local.

- Desde Unity: Window > General > Test Runner > EditMode > Run All.
- Por consola:

```
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults resultados.xml
```

## Créditos

DOTween (Demigiant) para las animaciones del menú.
