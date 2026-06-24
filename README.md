# xml2pdf

## 中文说明

### 项目简介

**xml2pdf** 是一个基于 .NET 开发的发票转换工具，用于将符合欧洲电子发票标准的 XML 文件转换为可阅读、可打印的 PDF 文件。

随着欧盟电子发票（E-Invoicing）政策的推广，越来越多的企业将采用结构化 XML 格式进行发票交换，例如：

* UBL Invoice 2.x
* EN16931
* PEPPOL BIS Billing
* Cross Industry Invoice (CII)

虽然 XML 格式便于系统间自动处理，但不方便人工查看和归档。

本项目的目标是：

> 将欧盟标准电子发票 XML 自动转换为格式清晰的 PDF 发票文档。

---

### 支持格式

当前支持：

* UBL Invoice 2.x
* UN/CEFACT CrossIndustryInvoice (CII)

未来计划支持：

* PEPPOL BIS Billing 3.0
* XRechnung
* ZUGFeRD / Factur-X
* EN16931 扩展格式

---

### 功能特点

* 自动扫描运行目录中的 XML 文件
* 自动识别发票格式（UBL / CII）
* 提取买方、卖方、交付地址等信息
* 提取发票明细行
* 提取税额及总金额
* 自动生成对应 PDF
* 无需安装 Office
* 支持批量转换

---

### 系统要求

* .NET 8.0 或更高版本

---

### 编译

```bash
dotnet restore
dotnet build -c Release
```

---

### 运行

将 XML 文件放入程序运行目录：

```text
xml2pdf.exe
invoice1.xml
invoice2.xml
invoice3.xml
```

执行：

```bash
dotnet run
```

或：

```bash
xml2pdf.exe
```

生成结果：

```text
invoice1.pdf
invoice2.pdf
invoice3.pdf
```

---



### 开源协议

MIT License

---

## Deutsche Dokumentation

### Projektbeschreibung

**xml2pdf** ist ein .NET-basiertes Werkzeug zur Konvertierung elektronischer Rechnungen im XML-Format in PDF-Dokumente.

Im Rahmen der Digitalisierung und der EU-weiten Einführung der elektronischen Rechnungsstellung werden strukturierte XML-Formate zunehmend verpflichtend eingesetzt.

Typische Formate sind:

* UBL Invoice 2.x
* EN16931
* PEPPOL BIS Billing
* Cross Industry Invoice (CII)

XML-Dateien eignen sich hervorragend für die automatische Verarbeitung zwischen Systemen, sind jedoch für Menschen schwer lesbar.

Dieses Projekt ermöglicht daher die automatische Umwandlung solcher XML-Rechnungen in übersichtliche PDF-Dokumente.

---

### Unterstützte Formate

Derzeit unterstützt:

* UBL Invoice 2.x
* Cross Industry Invoice (CII)

Geplante Erweiterungen:

* PEPPOL BIS Billing 3.0
* XRechnung
* ZUGFeRD / Factur-X
* EN16931 Erweiterungen

---

### Funktionen

* Automatische Erkennung von XML-Rechnungsformaten
* Unterstützung von UBL und CII
* Extraktion von Rechnungsdaten
* Extraktion von Lieferanten- und Kundendaten
* Darstellung von Rechnungspositionen
* Berechnung und Anzeige von Steuer- und Gesamtbeträgen
* Automatische PDF-Erstellung
* Stapelverarbeitung mehrerer XML-Dateien

---

### Voraussetzungen

* .NET 8.0 oder höher

---

### Kompilierung

```bash
dotnet restore
dotnet build -c Release
```

---

### Verwendung

XML-Dateien in das Ausführungsverzeichnis kopieren:

```text
xml2pdf.exe
rechnung.xml
```

Anschließend ausführen:

```bash
dotnet run
```

oder:

```bash
xml2pdf.exe
```

Die erzeugten PDF-Dateien befinden sich anschließend im selben Verzeichnis:

```text
rechnung.pdf
```

---

### Ziel des Projekts

Dieses Projekt unterstützt Unternehmen bei der Einführung der elektronischen Rechnungsstellung gemäß den zukünftigen EU-Anforderungen und ermöglicht gleichzeitig eine einfache visuelle Darstellung der Rechnungsdaten als PDF.

---

### Lizenz

MIT License
