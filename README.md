# Research-on-Thermal-Error-Compensation-of-Machine-Tools
This is the project database for Project “Research on Thermal Error Compensation of Machine Tools”.

## Project Abstract
Thermal error is one of the primary factors affecting the machining precision of machine tools. During CNC machining, heat from the spindle, 
motor, and Z-axis lead screw propagates via conduction, producing non-uniform temperature fields, component distortion, and hence thermal error. 
Generally, there are two approaches to mitigate thermal error: error prevention and error compensation. Error prevention involves improving design, 
precision manufacturing, and optimizing machine structure. However, for highprecision applications, this approach can lead to significantly increased costs. 
In contrast, error compensation is a more cost-effective and practical solution, involving the construction of accurate thermal error models, generating 
compensation values equal to the real errors, and feeding them into the machine-tool servo system to achieve error correction.    

A large number of students and researchers have conducted in-depth studies on thermal error modeling and compensation for machine tools. 
The vast majority of this work is grounded in data-driven approaches to thermal error modeling. Early studies employed methods such as 
backpropagation neural networks (BPNN) and support vector machines (SVM) for thermal error prediction and achieved promising results. 
In recent years, more scholars have found that recurrent neural networks (RNNs) and their variants perform particularly well on this task, 
primarily because thermal error exhibits time-series characteristics and temporal dependencies. Consequently, architectures such as long 
short-term memory networks (LSTMN) and gated recurrent units (GRU) have a natural advantage for handling these problems. Meanwhile, the 
development of digital-twin methodologies has laid the foundation for building thermal error compensation systems. An increasing number 
of researchers have incorporated the digital-twin concept into compensation frameworks to provide data monitoring, 
human–machine interaction, and compensation control, substantially advancing the maturity of such systems.   

Two important directions in our research are transfer learning and physics-based embedding for thermal error models. 
Transfer learning aims to enable deep-learning-based thermal error models to attain high accuracy on new machining processes 
or machine types using only a small amount of new data, thereby reducing model adaptation cost. Physics-based embedding 
primarily involves incorporating finite element analysis (FEA) or other mathematical representations to supply physical 
features or constraints to data-driven models.

## Repository structure
### C#
1. The 3 .ddl files (EZNcAut312.dll, EZSockets.dll, Interop.EZNCAUTLib.dll) under the MtcLibrary1 directory are necessary. Please add them as project dependencies.
2. Install the following packages in the NuGet tool of Visual Studio 2022.
![img.png](img.png)
#### Compensation 
##### MTC202501
The main program for the thermal error compensation of the machine tool used for edge deployment.
##### MtcLibrary1
The function library for the thermal error compensation of the machine tool used for edge deployment.

### Data
Temperature and thermal displacement data, which are used for importing into MySQL.

### Figure
#### cutting experiments
Figures related to cutting experiments and precision measurements
#### data collector
The data collector developed in this project.
#### Tem & Dis measurement point
Figures of the temperature and displacement measurement points show the installation method and location of the sensors.
#### Thermal Error Compensation Terminal
The TE compensation Terminal developed in this project.

### Python
#### LSTM_RES_CNN
The architecture and parameters of the Long Short-Term Memory Residual Convolutional Neural Network (LSTM_RES_CNN).