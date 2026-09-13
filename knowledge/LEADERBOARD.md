# Leaderboard

## Overall (total corpus, complete strategies only)

| rank | strategy | total ratio | saved |
|---|---|---|---|
| 1 | gen2.x-auto | 3.016x | 66.85% |
| 2 | gen2.auto | 3.014x | 66.82% |
| 3 | gen1.media-auto | 2.740x | 63.50% |
| 4 | gen1.media-pngf | 2.521x | 60.34% |
| 5 | gen1.text-token | 2.468x | 59.48% |
| 6 | brotli-11 | 2.348x | 57.40% |
| 7 | gen1.media-lpc | 2.281x | 56.16% |
| 8 | bz2-9 | 2.129x | 53.04% |
| 9 | gen1.text-lzmax | 2.127x | 52.99% |
| 10 | lzma-6 | 2.124x | 52.91% |
| 11 | lzma-9e | 2.122x | 52.87% |
| 12 | gen2.x-struct | 2.115x | 52.72% |
| 13 | gen1.struct-csv | 2.069x | 51.66% |
| 14 | gen1.struct-logs | 2.063x | 51.53% |
| 15 | zstd-22 | 2.021x | 50.53% |
| 16 | zstd-19 | 2.020x | 50.50% |
| 17 | gzip-9 | 1.925x | 48.04% |
| 18 | zstd-3 | 1.881x | 46.83% |

## Per file

### audio.pcm

| strategy | ratio | saved | comp MB/s | decomp MB/s | tag |
|---|---|---|---|---|---|
| gen1.media-auto | 1.533x | 34.76% | 0.1 | 1.2 | gen1-media |
| gen1.media-lpc | 1.533x | 34.76% | 0.4 | 1.2 | gen1-media |
| gen2.auto | 1.533x | 34.76% | 0.1 | 1.2 | gen2-auto |
| gen2.x-auto | 1.533x | 34.76% | 0.1 | 1.3 | gen2-crossover |
| gen1.media-pngf | 1.188x | 15.80% | 0.1 | 17.0 | gen1-media |
| brotli-11 | 1.184x | 15.51% | 0.2 | 82.6 | gen0 |
| gen1.text-token | 1.184x | 15.51% | 0.2 | 75.3 | gen1-text |
| bz2-9 | 1.157x | 13.54% | 10.4 | 15.5 | gen0 |
| gen1.text-lzmax | 1.054x | 5.09% | 2.7 | 14.8 | gen1-text |
| lzma-6 | 1.024x | 2.33% | 3.5 | 15.1 | gen0 |
| lzma-9e | 1.022x | 2.13% | 1.4 | 14.8 | gen0 |
| gzip-9 | 1.009x | 0.89% | 29.1 | 209.2 | gen0 |
| zstd-19 | 1.009x | 0.85% | 9.7 | 1158.8 | gen0 |
| zstd-22 | 1.009x | 0.85% | 10.7 | 1177.3 | gen0 |
| gen2.x-struct | 1.009x | 0.85% | 8.7 | 1301.9 | gen2-crossover |
| gen1.struct-csv | 1.009x | 0.85% | 7.6 | 567.3 | gen1-structured |
| gen1.struct-logs | 1.009x | 0.85% | 9.9 | 733.1 | gen1-structured |
| zstd-3 | 1.000x | -0.00% | 948.2 | 5011.0 | gen0 |

### code.py

| strategy | ratio | saved | comp MB/s | decomp MB/s | tag |
|---|---|---|---|---|---|
| gen1.text-token | 408.324x | 99.76% | 0.3 | 42.9 | gen1-text |
| gen2.auto | 407.900x | 99.75% | 0.1 | 43.2 | gen2-auto |
| gen2.x-auto | 407.900x | 99.75% | 0.1 | 43.3 | gen2-crossover |
| bz2-9 | 182.298x | 99.45% | 7.7 | 94.8 | gen0 |
| gen1.text-lzmax | 156.847x | 99.36% | 1.0 | 602.3 | gen1-text |
| lzma-9e | 151.237x | 99.34% | 0.8 | 657.9 | gen0 |
| brotli-11 | 134.387x | 99.26% | 1.3 | 1790.0 | gen0 |
| zstd-22 | 130.723x | 99.24% | 0.4 | 3170.5 | gen0 |
| lzma-6 | 129.347x | 99.23% | 11.0 | 767.0 | gen0 |
| zstd-19 | 102.320x | 99.02% | 1.6 | 2119.8 | gen0 |
| gen1.media-auto | 102.293x | 99.02% | 0.2 | 1820.7 | gen1-media |
| gen1.media-lpc | 102.293x | 99.02% | 0.2 | 2743.2 | gen1-media |
| gen1.media-pngf | 102.293x | 99.02% | 0.4 | 2797.8 | gen1-media |
| gen2.x-struct | 102.293x | 99.02% | 1.6 | 7199.4 | gen2-crossover |
| gen1.struct-csv | 102.214x | 99.02% | 1.6 | 1541.5 | gen1-structured |
| gen1.struct-logs | 102.214x | 99.02% | 1.5 | 3978.3 | gen1-structured |
| zstd-3 | 101.032x | 99.01% | 1430.4 | 4395.8 | gen0 |
| gzip-9 | 91.573x | 98.91% | 141.2 | 1216.0 | gen0 |

### image.rgb

| strategy | ratio | saved | comp MB/s | decomp MB/s | tag |
|---|---|---|---|---|---|
| gen1.media-auto | 1.851x | 45.99% | 0.4 | 3.5 | gen1-media |
| gen1.media-pngf | 1.851x | 45.99% | 0.4 | 3.5 | gen1-media |
| gen2.auto | 1.851x | 45.99% | 0.1 | 3.6 | gen2-auto |
| gen2.x-auto | 1.851x | 45.99% | 0.1 | 3.6 | gen2-crossover |
| brotli-11 | 1.372x | 27.14% | 0.1 | 90.4 | gen0 |
| gen1.text-token | 1.372x | 27.14% | 0.1 | 83.9 | gen1-text |
| lzma-6 | 1.154x | 13.37% | 3.9 | 15.0 | gen0 |
| lzma-9e | 1.154x | 13.31% | 2.0 | 14.9 | gen0 |
| gen1.text-lzmax | 1.122x | 10.91% | 2.7 | 15.5 | gen1-text |
| zstd-19 | 1.020x | 1.93% | 5.2 | 569.8 | gen0 |
| zstd-22 | 1.020x | 1.93% | 7.4 | 773.9 | gen0 |
| gen1.media-lpc | 1.020x | 1.93% | 1.7 | 604.1 | gen1-media |
| gen2.x-struct | 1.020x | 1.93% | 8.5 | 797.6 | gen2-crossover |
| gen1.struct-csv | 1.020x | 1.93% | 7.2 | 525.4 | gen1-structured |
| gen1.struct-logs | 1.020x | 1.93% | 7.9 | 521.2 | gen1-structured |
| bz2-9 | 1.018x | 1.73% | 9.9 | 15.5 | gen0 |
| gzip-9 | 1.017x | 1.71% | 31.8 | 190.7 | gen0 |
| zstd-3 | 1.008x | 0.85% | 421.1 | 1732.2 | gen0 |

### logs.jsonl

| strategy | ratio | saved | comp MB/s | decomp MB/s | tag |
|---|---|---|---|---|---|
| gen2.x-auto | 8.377x | 88.06% | 0.1 | 7.6 | gen2-crossover |
| gen2.x-struct | 8.377x | 88.06% | 0.7 | 7.5 | gen2-crossover |
| gen1.struct-logs | 8.342x | 88.01% | 3.2 | 68.9 | gen1-structured |
| gen2.auto | 8.342x | 88.01% | 0.1 | 68.6 | gen2-auto |
| bz2-9 | 6.651x | 84.97% | 9.1 | 36.8 | gen0 |
| gen1.text-token | 6.651x | 84.97% | 0.1 | 31.8 | gen1-text |
| brotli-11 | 5.676x | 82.38% | 0.5 | 383.4 | gen0 |
| lzma-6 | 5.603x | 82.15% | 2.7 | 71.7 | gen0 |
| gen1.text-lzmax | 5.592x | 82.12% | 2.0 | 69.0 | gen1-text |
| lzma-9e | 5.579x | 82.08% | 1.9 | 69.9 | gen0 |
| zstd-19 | 5.506x | 81.84% | 2.6 | 1159.7 | gen0 |
| zstd-22 | 5.506x | 81.84% | 2.6 | 1150.3 | gen0 |
| gen1.media-auto | 5.506x | 81.84% | 0.2 | 1123.8 | gen1-media |
| gen1.media-lpc | 5.506x | 81.84% | 0.3 | 955.7 | gen1-media |
| gen1.media-pngf | 5.506x | 81.84% | 0.3 | 1040.6 | gen1-media |
| gen1.struct-csv | 5.506x | 81.84% | 2.5 | 902.0 | gen1-structured |
| gzip-9 | 4.668x | 78.58% | 13.0 | 331.5 | gen0 |
| zstd-3 | 4.577x | 78.15% | 226.9 | 997.2 | gen0 |

### random.bin

| strategy | ratio | saved | comp MB/s | decomp MB/s | tag |
|---|---|---|---|---|---|
| gen1.text-token | 1.000x | -0.00% | 1.0 | 3822.0 | gen1-text |
| gen2.auto | 1.000x | -0.00% | 0.3 | 3367.6 | gen2-auto |
| gen2.x-auto | 1.000x | -0.00% | 0.3 | 3473.4 | gen2-crossover |
| brotli-11 | 1.000x | -0.00% | 2.7 | 1620.7 | gen0 |
| zstd-19 | 1.000x | -0.01% | 13.1 | 4754.1 | gen0 |
| zstd-22 | 1.000x | -0.01% | 13.7 | 4170.5 | gen0 |
| zstd-3 | 1.000x | -0.01% | 1901.8 | 5827.9 | gen0 |
| gen1.media-auto | 1.000x | -0.01% | 0.6 | 1524.5 | gen1-media |
| gen1.media-lpc | 1.000x | -0.01% | 2.1 | 1702.7 | gen1-media |
| gen1.media-pngf | 1.000x | -0.01% | 0.7 | 1265.4 | gen1-media |
| gen1.text-lzmax | 1.000x | -0.01% | 2.1 | 904.7 | gen1-text |
| gen2.x-struct | 1.000x | -0.01% | 11.3 | 8107.9 | gen2-crossover |
| gen1.struct-csv | 1.000x | -0.01% | 8.5 | 3427.8 | gen1-structured |
| gen1.struct-logs | 1.000x | -0.01% | 9.7 | 2836.3 | gen1-structured |
| lzma-6 | 1.000x | -0.03% | 4.1 | 1202.8 | gen0 |
| lzma-9e | 1.000x | -0.03% | 2.2 | 687.7 | gen0 |
| gzip-9 | 1.000x | -0.04% | 38.9 | 1544.2 | gen0 |
| bz2-9 | 0.994x | -0.61% | 9.3 | 17.5 | gen0 |

### series.csv

| strategy | ratio | saved | comp MB/s | decomp MB/s | tag |
|---|---|---|---|---|---|
| gen2.x-auto | 11.549x | 91.34% | 0.1 | 1.5 | gen2-crossover |
| gen2.x-struct | 11.549x | 91.34% | 0.4 | 1.5 | gen2-crossover |
| gen1.struct-csv | 11.336x | 91.18% | 1.8 | 8.7 | gen1-structured |
| gen2.auto | 11.335x | 91.18% | 0.1 | 8.9 | gen2-auto |
| gen1.text-token | 7.903x | 87.35% | 0.2 | 7.0 | gen1-text |
| gen1.text-lzmax | 6.042x | 83.45% | 2.2 | 60.5 | gen1-text |
| lzma-9e | 6.039x | 83.44% | 2.1 | 61.0 | gen0 |
| lzma-6 | 6.035x | 83.43% | 2.8 | 62.2 | gen0 |
| brotli-11 | 5.986x | 83.29% | 0.5 | 281.5 | gen0 |
| zstd-19 | 5.518x | 81.88% | 3.3 | 528.4 | gen0 |
| zstd-22 | 5.518x | 81.88% | 3.3 | 507.3 | gen0 |
| gen1.media-auto | 5.518x | 81.88% | 0.1 | 511.8 | gen1-media |
| gen1.media-lpc | 5.518x | 81.88% | 0.3 | 489.9 | gen1-media |
| gen1.media-pngf | 5.518x | 81.88% | 0.2 | 447.7 | gen1-media |
| gen1.struct-logs | 5.517x | 81.88% | 3.0 | 506.4 | gen1-structured |
| bz2-9 | 3.994x | 74.96% | 14.8 | 37.8 | gen0 |
| gzip-9 | 3.427x | 70.82% | 3.6 | 252.8 | gen0 |
| zstd-3 | 3.035x | 67.06% | 142.7 | 635.4 | gen0 |

### text.txt

| strategy | ratio | saved | comp MB/s | decomp MB/s | tag |
|---|---|---|---|---|---|
| gen1.text-token | 9.047x | 88.95% | 0.1 | 0.4 | gen1-text |
| gen2.auto | 9.046x | 88.95% | 0.1 | 0.3 | gen2-auto |
| gen2.x-auto | 9.046x | 88.95% | 0.1 | 0.3 | gen2-crossover |
| bz2-9 | 7.990x | 87.48% | 12.2 | 35.3 | gen0 |
| zstd-19 | 5.789x | 82.72% | 2.5 | 874.3 | gen0 |
| zstd-22 | 5.789x | 82.72% | 2.5 | 830.1 | gen0 |
| gen1.media-auto | 5.788x | 82.72% | 0.2 | 825.5 | gen1-media |
| gen1.media-lpc | 5.788x | 82.72% | 0.4 | 760.9 | gen1-media |
| gen1.media-pngf | 5.788x | 82.72% | 0.3 | 709.1 | gen1-media |
| gen2.x-struct | 5.788x | 82.72% | 2.4 | 797.7 | gen2-crossover |
| gen1.struct-csv | 5.788x | 82.72% | 2.3 | 691.6 | gen1-structured |
| gen1.struct-logs | 5.788x | 82.72% | 2.2 | 871.1 | gen1-structured |
| gen1.text-lzmax | 5.784x | 82.71% | 2.0 | 102.0 | gen1-text |
| lzma-6 | 5.766x | 82.66% | 2.4 | 107.1 | gen0 |
| lzma-9e | 5.760x | 82.64% | 2.0 | 101.6 | gen0 |
| brotli-11 | 5.704x | 82.47% | 0.5 | 502.9 | gen0 |
| gzip-9 | 4.946x | 79.78% | 12.5 | 334.5 | gen0 |
| zstd-3 | 4.430x | 77.43% | 234.2 | 896.9 | gen0 |

