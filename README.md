# PB_Clientes

WebAPI Rest com a criação de novos usuarios. Tem também o worker que fica lendo a tabela outbox para desacolar a api da escrita na mensageria rabbitMQ.

## ⚙️ Como rodar localmente
 1. **ter o docker desktop instalado na maq**, caso nao tenha seguir as instruções pelo [link](https://www.docker.com/get-started/)
 2. **subir as imagens do banco de dados, do rabbitMQ e depois da observabilidade**

    a) ir para a pasta /infra do projeto via cmd

    b) rodar o comando
    ```cmd
    docker compose up -d
    ```

    c) ir para a pasta /infra/observability via cmd

    d) rodar o mesmo comando do passo b) para subir as imagens necessárias para a observabilidade

3. **rodar as migrations do MS PB_Clientes e do MS PB_Cartoes e do PB_Orquestrador**
4. **integrar o nugget PB_Common**

   a) na sln vá em manage Nuget Packages

   b) clique no simbolo de configurações
   
   <img width="289" height="53" alt="image" src="https://github.com/user-attachments/assets/dde69034-efc2-4226-8654-105b9b3bcd43" />

   c)clique no simbolo "+"
   
   <img width="805" height="380" alt="image" src="https://github.com/user-attachments/assets/e3c1e5e3-0345-4fb1-8914-2d7d6d70a60a" />

   d) preencha as informações de acordo com o caminho onde compilou a sln PB_Common
   
   <img width="787" height="135" alt="image" src="https://github.com/user-attachments/assets/b7395555-f098-4a51-bc59-b535f900ca19" />

5. Rodar a sln

## 💡 Observações

A arquitetura utiliza Outbox Pattern para garantir consistência entre a base de dados e o envio de eventos RabbitMQ.

A observabilidade foi configurada com OpenTelemetry + Grafana + Tempo, permitindo rastrear o fluxo completo de eventos distribuídos.
