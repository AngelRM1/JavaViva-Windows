# JavaViva Windows — Notas técnicas (desarrolladores)

El README principal es la guía para usuarios. Este archivo guarda lo
técnico: cómo se compila, cómo funciona por dentro y qué cambió en
cada versión. El historial completo de cada versión también queda en
el historial de commits de este repositorio.

## Compilar

Necesitas el **.NET 8 SDK** (gratis, de Microsoft). Desde `src/`:

```bash
dotnet publish -r win-x64 -c Release -o ../publish
```

El proyecto es `src/JavaViva.csproj` (`net8.0-windows`, WinForms,
`EnableWindowsTargeting=true` para compilar desde Linux). El
resultado es `publish/JavaViva.exe`, un solo archivo autocontenido
(no necesita tener .NET instalado para correr). El build de este
entorno es `bash build.sh` (dotnet en `~/dotnet/dotnet`).

El `.exe` compilado **no juega solo**: necesita al lado el motor y
un Java, que no están en el repo (vienen en el ZIP del release):

- **El motor:** `engine/freej2me_plus.jar` — FreeJ2ME-Plus v1.53,
  desde https://github.com/hex007/freej2me/releases.
- **Un Java:** JavaViva lo busca en este orden:
  `runtime/windows-x64/` junto al `.exe` (un JRE Temurin 21
  descomprimido ahí, con `bin/java.exe` dentro), el Java de tu PC
  (PATH o Program Files), o uno elegido a mano en Configuración.
  Descarga gratis: Adoptium Temurin JRE 21 para Windows x64 —
  https://adoptium.net/temurin/releases/.

## Empaquetar el release

```bash
cp README.md publish/README.md
cp README.md publish/Juegos/LEEME-Juegos.txt
zip -qr JavaViva-vX.Y.Z.zip publish
```

El ZIP lleva `JavaViva.exe` (~155 MB), `engine/`, `runtime/` y
`Juegos/` (vacía: el paquete no incluye juegos). La biblioteca
empieza vacía.

## Cómo funciona por dentro

- **Motor:** FreeJ2ME-Plus v1.53 (GPLv3). Se lanza como
  `java -jar freej2me_plus.jar <juego> 0 <ancho> <alto> <escala>`
  (el `0` es ventana, no pantalla completa).
- **Biblioteca:** `%AppData%\JavaViva\library.json`; datos por juego
  en `%AppData%\JavaViva\datos\<nombre>`. Al arrancar se limpian las
  entradas de archivos que ya no existen y los duplicados.
- **Resolución automática:** lee el manifiesto del `.jar` y busca
  `ifone.png` / `glu.png` (su tamaño es la resolución del juego);
  si no, usa la imagen más grande del `.jar`.
- **Configuración por juego:** resolución, escala 1–3x, rotación,
  FPS, sonido, luz de fondo, perfil de teléfono, M3G y
  compatibilidad. Se guarda por juego y se escribe en el
  `game.conf` que lee el motor.
- **Proceso del juego (v1.1.6):** FreeJ2ME-Plus se relanza en otro
  JVM al arrancar: el `java.exe` que se lanza sale enseguida
  (código 0) y el juego corre en un `java.exe` hijo. Por eso
  JavaViva sigue al hijo (`GameProcessFinder`: por fecha de
  arranque y ventana; si el motor no se relanza, se queda con el
  lanzador). El manejador de salida ignora procesos viejos y al
  lanzar con un juego corriendo pregunta si detenerlo primero.
- **Actualizaciones:** al arrancar revisa el último release de
  `AngelRM1/JavaViva-Windows` en GitHub (API pública, sin cuenta);
  si hay una versión más nueva, el botón del encabezado se pone
  verde. Sin internet no molesta.
- **Donaciones:** pestaña con enlaces (PayPal, Buy Me a Coffee,
  nouslogic.xyz/dona-un-cafe). Voluntarias, no desbloquean nada.

## Historial (resumido)

- **v1.1.7 (2026-11-24):** sin sección de terminal (las pestañas
  ocupan toda la ventana); todos los botones de acción 240x40
  (las rejillas uniformes por dentro con el mismo alto 40);
  "Buscar actualizaciones" en el encabezado, a la derecha de las
  etiquetas de Java/versión.
- **v1.1.6 (2026-11-24):** se sigue al proceso hijo del juego
  (antes el registro decía "se cerró" con el juego en pantalla,
  Detener no mataba al juego real y la captura pedía pulsar play);
  pregunta antes de lanzar un segundo juego; la captura espera a
  que la ventana exista; pestañas sin fondo blanco; botón de
  actualizaciones con más aire y ventana ajustada a la pantalla.
- **v1.1.5 (2026-11-24):** favoritos + búsqueda, captura de
  pantalla (PNG a `Capturas\`) y respaldo de la biblioteca
  (exportar/importar JSON).
- **v1.1.4 (2026-11-24):** aviso de nueva versión al arrancar.
- **v1.1.3 (2026-11-24):** los botones de Biblioteca por fin se
  agregan a la pestaña (faltaban desde v1.0); limpieza de la lista
  al arrancar; resolución en Biblioteca; pestaña Donaciones.
- **v1.1.2 (2026-11-24):** eliminar juegos y limpiar la lista;
  botón Iniciar juego en Biblioteca.
- **v1.1.1 (2026-11-24):** modo oscuro terminado; botón Iniciar
  juego en Configuración; carpeta `Juegos/` vacía.
- **v1.1.0 (2026-11-24):** modo oscuro completo (título, scrolls,
  lista, desplegables).
- **v1.0.2 (2026-11-19):** corrección de la revisión: `game.conf`
  en la carpeta correcta, dígitos del teclado como `{NUMPADn}`,
  perfiles con sus valores internos, sonido aplicado siempre,
  `.jar` inválidos rechazados con motivo.
- **v1.0.1 (2026-11-18):** faltaba el argumento de pantalla
  completa en la línea de comandos (pantalla negra).
- **v1.0 (2026-11-17):** primera versión.

## Verificación

- Build limpio con .NET SDK 8.0.428 para `win-x64`: 0 errores,
  0 advertencias. El `.exe` es PE32+ GUI x86-64.
- Resolución automática y compatibilidad verificadas con `.jar`
  reales de 176x220 y 240x320 (manifiesto, imagen mayor, arreglo
  de repintado).
- La prueba real de un juego corriendo en un PC con Windows es el
  paso que falta: este entorno no tiene Windows.
- El `game.conf` generado se verificó clave por clave contra el
  código fuente de FreeJ2ME-Plus v1.53 (`Config.java`).
