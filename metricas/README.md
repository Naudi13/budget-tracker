# Medición de métricas

`medir.py` mide el código y escribe `docs/metricas/<nombre>/resultados.md` junto con sus CSV.
Cada medición va en su propia carpeta: la inicial está en
[`docs/metricas/antes`](../docs/metricas/antes/resultados.md). Las definiciones de cada métrica están en
[`docs/metricas/fichas.md`](../docs/metricas/fichas.md).

## Requisitos

Python 3.10 o superior, .NET SDK 10, Git y, para medir el inicio de sesión, Docker.

```bash
python -m pip install -r metricas/requirements.txt
```

## Medir

Desde la raíz del repositorio:

```bash
python metricas/medir.py --nombre antes
```

Para probar además el límite de intentos de inicio de sesión (RNF-02), con la aplicación en marcha:

```bash
python metricas/medir.py --nombre antes --base-url http://localhost:8080
```

Para contrastar dos mediciones, por ejemplo `antes` y `despues`:

```bash
python metricas/comparar.py antes despues
```

El job `complexity` del CI aplica en cada pull request el mismo umbral de RNF-01 con `lizard`.

Sin `--base-url` se conserva la última medición del inicio de sesión. Con `--no-build` no se recompilan
la API ni las migraciones.
