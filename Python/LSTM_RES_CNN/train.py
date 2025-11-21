import tensorflow as tf
import numpy as np
import data_prepare as dp
import time
input_data,output_data=dp.eva_or_train('train')
input_data_lstm,output_data_lstm = [],[]
for i in range(0,len(input_data) - 32,16):
    input_data_lstm.append(input_data[i : i + 32])
    output_data_lstm.append(output_data[i + 32])
input_data_lstm=np.array(input_data_lstm)
output_data_lstm=np.array(output_data_lstm)
batch_size=64
def create_model():
    inputs = tf.keras.Input(shape=(32, 6))
    x = inputs
    x = tf.keras.layers.LSTM(16, return_sequences=True, activation='relu')(x)  # 32 64
    x_flat=tf.keras.layers.Reshape((1,512))(x)
    x1 = tf.keras.layers.Conv1D(128, 16, 1)(x)
    x1 = tf.keras.layers.Activation('relu')(x1)
    x1 = tf.keras.layers.Conv1D(256, 10, 1)(x1)
    x1 = tf.keras.layers.Conv1D(256, 6, 1)(x1)
    x1 = tf.keras.layers.Conv1D(512, 3, 1)(x1)
    x1 = tf.keras.layers.Activation('relu')(x1)
    x_add_1 = tf.add(x1, x_flat)
    x2=tf.keras.layers.Reshape((32, 16))(x_add_1)
    x2 = tf.keras.layers.Conv1D(128, 13, 1)(x2)
    x2 = tf.keras.layers.Activation('relu')(x2)
    x2 = tf.keras.layers.Conv1D(256, 11, 1)(x2)
    x2 = tf.keras.layers.Conv1D(256, 7, 1)(x2)
    x2 = tf.keras.layers.Conv1D(512, 4, 1)(x2)
    x2 = tf.keras.layers.Activation('relu')(x2)
    x_add_2 = tf.add(x2, x_add_1)
    x3 = tf.keras.layers.Reshape((32, 16))(x_add_2)
    x3 = tf.keras.layers.Conv1D(128, 16, 1)(x3)
    x3 = tf.keras.layers.Activation('relu')(x3)
    x3 = tf.keras.layers.Conv1D(256, 10, 1)(x3)
    x3 = tf.keras.layers.Conv1D(256, 6, 1)(x3)
    x3 = tf.keras.layers.Conv1D(512, 3, 1)(x3)
    x3 = tf.keras.layers.Activation('relu')(x3)
    x_add_3 = tf.add(x3, x_add_2)
    x = tf.keras.layers.GlobalAveragePooling1D()(x_add_3)
    output = tf.keras.layers.Dense(5)(x)
    model = tf.keras.Model(inputs=inputs, outputs=output)
    return model

model=create_model()
loss_fn = tf.keras.losses.MeanSquaredError()
optimizer = tf.keras.optimizers.SGD(learning_rate=0.01,nesterov=True)  # Nesterov动量
@tf.function
def train_step(inputs, targets):
    with tf.GradientTape() as tape:
        predictions = model(inputs, training=True)
        print(predictions)
        loss_value = loss_fn(targets, predictions)
    grads = tape.gradient(loss_value, model.trainable_variables)
    optimizer.apply_gradients(zip(grads, model.trainable_variables))
    return loss_value

epochs = 20000 +1
for epoch in range(epochs):
    start_clock = time.time()
    for batch in range(0,len(input_data_lstm)-batch_size+1,batch_size):
        start = batch
        end = batch +  batch_size
        inputs =input_data_lstm[start:end]
        print(inputs.shape)
        targets = output_data_lstm[start:end]
        if batch==0:
            loss = train_step(inputs, targets)
        else:
            loss = loss+train_step(inputs, targets)
    if (epoch + 1) % 200 == 0:
        print('epoch {}, loss:{}, time of each epoch:{:.2f} secs'.format(epoch + 1, loss, time.time() - start_clock))
    if (epoch + 1) % 2000 == 0:
        path = './ckpt_20251011_1/' + str(epoch + 1)
        model.save(path)
