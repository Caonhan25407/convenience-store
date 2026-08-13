<script>

</script>

<template>
    <h1>Welcome</h1>
    <div class="container">
        <div class="item">Header</div>
        <div class="item">Sidebar</div>
        <div class="item">Content</div>
        <div class="item">Footer</div>
    </div>
</template>
    
<style scoped>

    .container {
        display: flex;
        gap: 5px;
        flex-wrap: wrap;
        align-content: start;
        
        }

        .item:nth-child(1) {
        width: 100%;
        height: 4rem;
        background-color: black;
        }

        .item:nth-child(2) {
        width: 20rem;
        height: 60rem;
        background-color: antiquewhite;
        }

        .item:nth-child(3) {
        flex-grow: 1;
        height: 60rem;
        background-color:antiquewhite;
        border: 3px;
        border-color: black;
        }

        .item:nth-child(4) {
        width: 100%;
        height: 4rem;
        background-color: black;
        

    }
</style>