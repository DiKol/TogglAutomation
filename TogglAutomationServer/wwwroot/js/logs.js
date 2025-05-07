const pageIndex = document.getElementById("page-index")
const pageMax = document.getElementById('page-max')
const nextPageButton = document.getElementById('next-page')
const backPageButton = document.getElementById('back-page')
var page = 0
var maxPage = 0
var lastSearchFilters = {page: 0}

async function getLogs(filters = {}) {
    try {
        lastSearchFilters = filters
        nextPageButton.style.visibility = 'hidden'
        backPageButton.style.visibility = 'hidden'
        const response = await fetch("/api/logs/get", {
            method: "POST",
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(filters)
        })
        const json = await response.json()
        const { page, pageLimit, count, logs } = json
        addLogsToTable(logs)
        console.log(page, pageLimit, count)
        
        maxPage = count % pageLimit === 0 ? count / pageLimit : Math.floor(count / pageLimit) + 1
        console.log(maxPage)
        pageIndex.innerHTML = (page + 1).toString()
        pageMax.innerHTML = maxPage.toString()
        if (page !== 0) backPageButton.style.visibility = null
        if (maxPage !== page + 1) nextPageButton.style.visibility = null
    } catch (e) {
        console.error(e)
    }
}

function next() {
    if (page + 1 === maxPage) return;

    page++;
    lastSearchFilters.page = page;
    getLogs(lastSearchFilters);
}

function back() {
    if (page === 0) return;

    page--
    lastSearchFilters.page = page;
    getLogs(lastSearchFilters)
}

function search() {
    page = 0
    const filters = getFilters()
    getLogs(filters)
}

getLogs(lastSearchFilters);