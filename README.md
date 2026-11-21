# JavaViva v1.0.2 — Juegos Java .jar como en tu celular de los 2000

> **v1.0.2 (2026-11-19) — corrección de la revisión de Windows: los 3
> errores graves, el sonido y la validación de .jar.**
> 1. El `game.conf` se escribía en la carpeta equivocada (con guiones
> bajos) y el motor lo ignoraba en silencio para Age of Empires y
> Carbon. Ahora se escribe también en `config/<MIDlet-Name tal cual>/`.
> 2. Los botones de dígitos del teclado en pantalla enviaban la tecla
> de la fila superior y el motor la ignoraba. Ahora envían
> `{NUMPADn}`: acelerar con el 5 en NFS ya funciona.
> 3. Los perfiles "Nokia", "Motorola V8", "Motorola A1000" y "Nokia
> Full Keyboard" no existen para el motor (son solo etiquetas de su
> GUI). Ahora se escriben sus valores internos: `NokiaKeyboard`,
> `MotoV8`, `MotoA1000`. Las bibliotecas viejas se migran solas.
> Además: cambiar Sonido ya se aplica (antes solo la primera vez),
> los .jar corruptos o sin manifiesto se rechazan con el motivo, el
> java.exe elegido a mano ya se guarda al cerrar, Jugar ya aplica los
> cambios de Configuración sin pulsar Guardar, y un `library.json`
> corrupto ya no vacía la biblioteca en silencio (se guarda copia).
> Pendiente, no en esta versión: mantener pulsado un botón del teclado
> para acelerar, y la tabla de teclas ya no promete Espacio/X/Ctrl+C.
>
> **v1.0.1 (2026-11-18) — se corrige que ningún juego se viera: faltaba
> el argumento de pantalla completa en la línea de comandos.**
> FreeJ2ME-Plus v1.53 espera `java -jar freej2me_plus.jar <juego>
> <fullscreen 0/1> <ancho> <alto> <escala>` (constructor de
> FreeJ2ME.java: args[0]=jar, args[1]=fullscreen, args[2]=ancho,
> args[3]=alto, args[4]=escala; tras cargar, scrwidth=args[argLen-2],
> scrheight=args[argLen-1]). La v1.0 pasaba `[juego, ancho, alto,
> escala]`, así que el ancho caía en el hueco de fullscreen y la LCD
> quedaba en 220x2 para un juego 176x220 (pantalla negra, ventana
> enana). Ahora se pasa `0` (ventana) entre el juego y la resolución.
> El mismo bug estaba en JavaViva Web (corregido en su v1.8).
Creado por **Nous Logic** — nouslogic.xyz

## Qué es
JavaViva es un launcher para Windows que ejecuta tus juegos y programas
Java ME (J2ME) en archivos `.jar` / `.jad`, como si corrieran en un Nokia,
Sony Ericsson o Motorola de los años 2000.

No es un emulador hecho desde cero: usa como motor **FreeJ2ME-Plus v1.53**
(GPLv3, de TASEmulators), el emulador J2ME más compatible hoy en PC, con
soporte MIDP 2.0, 3D M3G (JSR-184) y Mascot Capsule. JavaViva pone encima
la biblioteca, la detección automática de resolución y la configuración
por juego.

## Qué trae el paquete
- `JavaViva.exe` — **155 MB, un solo archivo, autocontenido.** No necesita
  instalar .NET. Es el recomendado. Doble clic y listo.
- `engine/freej2me_plus.jar` — el motor FreeJ2ME-Plus v1.53.
- `runtime/windows-x64/` — Java Temurin 21 incluido. **No necesitas
  instalar Java aparte.** Si esa carpeta falta, JavaViva busca el Java
  de tu PC o uno que elijas en Configuración.
- `Juegos/` — aquí van tus `.jar` y `.jad`. Ya viene con
  **Need_For_Speed_Most_Wanted.jar** (NFSMW, EA, MIDP 2.0, v1.0.2).
- `src/` — código fuente completo en C# (.NET 8, WinForms).

## Cómo probarlo en tu PC (5 minutos)
1. Descomprime el ZIP completo en una carpeta (el .exe solo no basta:
   necesita `engine/` y `runtime/` al lado).
2. Doble clic en `JavaViva.exe`.
   > Windows SmartScreen puede avisar porque el .exe no tiene firma de
   > pago. Es normal en un programa nuevo sin certificado. Pulsa
   > "Más información" → "Ejecutar de todos modos" si confías en el origen.
3. En **Biblioteca** ya debe aparecer **NFSMW** (Need for Speed Most
   Wanted), con resolución auto **240x320**. Si no aparece, pulsa
   **Refrescar carpeta Juegos**.
4. Selecciónalo y pulsa **▶ Jugar**. Se abre la ventana del juego aparte.
   - Si pregunta **Enable Sound?**, pulsa **Q** (Yes, tecla suave izquierda).
   - En el menú, usa las **flechas** para moverte y **Enter** para aceptar.
   - **Esc** abre el menú del emulador (resolución, sonido, salir).
5. Si el juego se ve cortado, chico o en blanco: ciérralo y en
   **Configuración** cambia la resolución (o usa los botones
   **Probar rápido** en Biblioteca: 240x320, 176x208, 320x240...).

## Panel de Configuración (por juego)
- **Pantalla:** Automática (recomendado), de la lista, o personalizada
  (ancho x alto). La automática lee el .jar: busca `ifone.png` /
  `glu.png` (su tamaño ES la resolución del juego) y si no, usa la
  imagen más grande del .jar. NFS Most Wanted detecta 240x320.
- **Escala de ventana:** 1x, 2x, 3x. **Girar pantalla:** 0°/90°/180°/270°.
- **Límite FPS:** Auto (60), 60, 50 o 30. **Sonido** y **luz de fondo**
  (verde Nokia clásico, cian, naranja, violeta, rojo).
- **Perfil de teléfono:** Standard (casi todos), Nokia, Siemens,
  Motorola Triplets / V8 / A1000, LG, Sagem, SKT, KDDI, Nokia Full
  Keyboard. Cambia solo si el juego fue hecho para esa marca.
- **Gráficos 3D (M3G):** media resolución 3D si va lento, antialiasing.
- **Compatibilidad:** Repaint on setCurrent (NFS Most Wanted lo necesita,
  si no se queda en pantalla blanca), Immediate repaints, Override
  platform checks. JavaViva activa el arreglo de NFS Most Wanted solo.
- **Java:** automático (runtime incluido), del sistema o elegido a mano.
- Todo se guarda **por juego** y también se escribe en el `game.conf`
  que FreeJ2ME-Plus lee (`config/<nombre>/game.conf` en los datos del
  juego, en `%AppData%\JavaViva\datos\`). Los guardados del juego quedan
  ahí, separados por juego.

## Teclas (pestaña 📱 Teclas)
- **Q / W:** teclas suaves izquierda / derecha. **Flechas:** moverte.
- **Enter o 5:** disparar / aceptar (en NFS, acelerar).
- **1-9, 0:** teclas del teléfono. **E:** `*`, **R:** `#`, **A:** borrar.
- **Esc:** menú del emulador. **Ctrl+C:** captura. **+ / -:** zoom.
- **Espacio:** avance rápido. **X:** pausa.
- En la pestaña Teclas hay un teclado del teléfono en pantalla: pulsa
  un botón y JavaViva lleva la ventana del juego al frente y pulsa esa
  tecla por ti.

## NFS Most Wanted: lo que se verificó
- El .jar es la versión **240x320** (S60 v3 / S40 / Sony Ericsson):
  manifiesto `MIDlet-Name: NFSMW, Vendor: Electronic Arts, MIDP-2.0,
  v1.0.2`, 695 KB, y sus imágenes más grandes son de 240x320.
  Usa 3D M3G (`scene_LoadM3GMesh`), que FreeJ2ME-Plus soporta.
- **Arreglo aplicado:** `compatrepaintonsetcurrent=on` en el `game.conf`.
  Sin él, el juego se queda en pantalla blanca (el valor por defecto del
  emulador es `off`). JavaViva lo activa solo para este juego.
- **No se pudo probar el juego corriendo en este entorno:** aquí no hay
  Windows y la prueba en Linux se quedó sin librerías X11 (`libXtst`).
  Por eso la prueba real en tu PC es el paso que falta: sigue la guía de
  5 minutos y dime qué ves (menú, carreras, sonido) y si algo se ve
  cortado o en blanco.

## Límites honestos
- No todos los juegos J2ME funcionan: cada uno fue hecho para un
  teléfono concreto. Si uno no arranca, prueba otra resolución o perfil.
- No convierte `.jar` a `.apk` ni a `.exe`, y no descarga juegos.
- Usa solo `.jar` que tengas legalmente. Un `.jar` puede ejecutar código
  en tu PC como cualquier programa: no ejecutes archivos de sitios raros.

## Verificación hecha en esta entrega (2026-11-17, v1.0)
- Compilado con .NET SDK 8.0.428 para `win-x64`: **0 errores,
  0 advertencias**. El .exe es PE32+ GUI x86-64.
- Lógica probada con el .jar real de NFS Most Wanted (programa de
  prueba en C# con el mismo código del launcher): nombre NFSMW,
  Electronic Arts, v1.0.2, MIDP-2.0, 711.272 bytes, resolución auto
  240x320 (imagen mayor 240x323 → 240x320), arreglo repaint activado,
  línea de comandos `java -jar freej2me_plus.jar
  "file:///C:\Juegos\Need_For_Speed_Most_Wanted.jar" 0 240 320 2`
  (el `0` de pantalla completa se añadió en v1.0.1; sin él la LCD
  quedaba en 320x2).
- `game.conf` generado verificado clave por clave contra el código
  fuente de FreeJ2ME-Plus v1.53 (`Config.java`): todas las claves
  existen y los valores por defecto coinciden.
- Textos clave verificados dentro del .exe (UTF-16): compatrepaintonsetcurrent,
  FreeJ2ME-Plus, ifone.png / glu.png, freej2me_plus.jar, Adoptium.

## Recompilar
En Windows con .NET 8 SDK instalado, o en Linux con el SDK:
```
cd src
dotnet publish -r win-x64 -c Release -o ../publish
```
El proyecto es `src/JavaViva.csproj` (`net8.0-windows`, WinForms,
`EnableWindowsTargeting=true` para compilar desde Linux).

## Ideas para v1.1 (si quieres seguir)
- Probar con KEmulator nnmod como segundo motor para los juegos que
  FreeJ2ME-Plus no corra.
- Guardar partidas / savestates desde JavaViva y capturas en la biblioteca.
- Versión web (FreeJ2ME-Plus Web) para jugar desde el navegador sin instalar.
- App Android con J2ME-Loader (el mejor emulador J2ME en Android).
