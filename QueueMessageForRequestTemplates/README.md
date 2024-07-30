# Шаблоны очереди сообщений
Шаблон решения с реализацией очереди сообщений для асинхронной обработки большого потока исходящих и входящих сообщений.

## Кейсы применения
Данный шаблон можно применять с минимальными модификациями в следующих случаях:

1. Отправка большого количества уведомлений пользователям (например, email-рассылки).
2. Интеграция с внешними API, требующими ограничения частоты запросов.
3. Обработка пакетных заданий (например, генерация отчетов).
4. Синхронизация данных между несколькими системами.
5. Реализация отложенных операций (например, автоматическое закрытие заявок через определенное время).
6. Балансировка нагрузки при высокочастотной обработке данных.

## Использование
### Импорт 
1. Импортировать шаблон решения в проект в качестве самостоятельного модуля.
2. Реализовать справочник настроек и методы для обеспечения взаимодействия с внешними системами (захват данных из файловой системы, прикладные методы для Сервиса интеграции и пр.).
3. Доработать метод создания нового сообщения в Справочнике сообщений CreateMessage (QueueMessageForRequestTemplates/DirRX.QueueMessageForRequest/DirRX.QueueMessageForRequest.Server/ModuleFunctions.cs).
4. Доработать асинхронный обработчик обработки сообщений ProcessingMessage (QueueMessageForRequestTemplates/DirRX.QueueMessageForRequest/DirRX.QueueMessageForRequest.Server/ModuleAsyncHandlers.cs). Асинхронный обработчик запускается при создании нового элемента очереди на сохранении.

### Экспорт
1. Реализовать справочник настроек и методы для обеспечения взаимодействия с внешними системами (выгрузка в файловое хранилище, отправка сообщения во внешнюю систему и пр.).
2. Доработать метод создания нового сообщения в Справочнике сообщений CreateMessage (QueueMessageForRequestTemplates/DirRX.QueueMessageForRequest/DirRX.QueueMessageForRequest.Server/ModuleFunctions.cs) и использовать этот метод в прикладной логике отправки объекта системы во внешнюю систему.
3. Доработать асинхронный обработчик отправки сообщений SendMessage (QueueMessageForRequestTemplates/DirRX.QueueMessageForRequest/DirRX.QueueMessageForRequest.Server/ModuleAsyncHandlers.cs)
4. При необходимости, реализовать логику обработки ошибок и повторных попыток.

## Основные компоненты

Реестр сообщений: QueueMessages

Методы создания и обработки сообщений (QueueMessageForRequestTemplates/DirRX.QueueMessageForRequest/DirRX.QueueMessageForRequest.Server/ModuleFunctions.cs):
 - Создание: CreateMessage 
 - Асинхронная отправка: SendMessageAsync 
 - Синхронная отправка: SendMessageSync 
 - Обработка ошибок: HandlingErrorSend 

Асинхронные обработчики (QueueMessageForRequestTemplates/DirRX.QueueMessageForRequest/DirRX.QueueMessageForRequest.Server/ModuleAsyncHandlers.cs):
 - Отправка сообщений: SendMessage
 - Обработка сообщений: ProcessingMessage 

Фоновые процессы (QueueMessageForRequestTemplates/DirRX.QueueMessageForRequest/DirRX.QueueMessageForRequest.Server/ModuleAsyncHandlers.cs):
 - Отправка сообщений из реестра во внешнюю систему: SendMessage 
 - Удаление обработанных сообщений: RemoveQueueMessages

Служебные методы (QueueMessageForRequestTemplates/DirRX.QueueMessageForRequest/DirRX.QueueMessageForRequest.Server/ModuleFunctions.cs):
 - Получение очереди сообщений: GetMessagesQueue
 - Очистка устаревших сообщений: GetMessagesQueueForDelete

Интеграционный метод (QueueMessageForRequestTemplates/DirRX.QueueMessageForRequest/DirRX.QueueMessageForRequest.Server/ModuleFunctions.cs)::
 - Создание сообщения через Сервис интеграции: CreateMessageQueue
