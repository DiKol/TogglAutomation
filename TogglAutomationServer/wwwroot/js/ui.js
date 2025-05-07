const allInputs = document.getElementsByClassName('extension-checkbox')
const logsTable = document.getElementById('logs-table')
/**@type {HTMLInputElement}*/
const fromDateInput = document.getElementById('from-date')
/**@type {HTMLInputElement}*/
const toDateInput = document.getElementById('to-date')
/**
 * Connected = 0,
    Disconnected = 1,
    WorkOnProject = 2,
    StoppedTimer = 3,
    Choose = 4,
    CallIncoming = 5,
    CallEnded = 6,
    CallMissedOrDeclined = 7,
    CallStarted = 8,
    WorkBeforeCall = 9,
    WorkAfterCall = 10,
    DoNothing = 11
 */
const actions = [
    "Connected",
    "Disconnected",
    "Work On Project",
    "Stopped Timer",
    "Choose Project",
    "Call Incoming",
    "Call Ended",
    "Call Missed or Declined",
    "Call Started",
    "Work Before Call",
    "Work After Call",
    "Do Nothing"
]


function selectAll() {
    for (let input of allInputs) {
        input.checked = true;
    }
}
function unselectAll() {
    for (let input of allInputs) {
        input.checked = false;
    }
}

function addLogsToTable(logs) {
    logsTable.innerHTML = ''
    let finalStr = `<tr>
                        <th>Extension</th>
                        <th>Date (m/d)</th>
                        <th>Action</th>
                        <th>Project</th>
                    </tr>`;
    for (let log of logs) {
        const { extension, date, logType, project, usingTheApp } = log
        console.log(project)
        const dateInstance = new Date(date)
        finalStr += `<tr>
                <td><div class="${usingTheApp ? 'using' : 'notusing'}"></div>${extension}</td>
                <td>${dateInstance.getMonth() + 1}/${dateInstance.getDate()} ${dateInstance.getHours()}:${dateInstance.getMinutes()}(${dateInstance.getSeconds()}s)</td>
                <td>${actions[logType]}</td>
                <td ${project == null ? '' : `style="color: ${project.color};"`}>${project == null ? '' : project.name}</td>
            </div>`
        console.log(log)
    }
    logsTable.innerHTML = finalStr
}

function getFilters() {
    const allExtensions = []
    let areAllChecked = true
    for (let input of allInputs) {
        if (!input.checked) {
            areAllChecked = false;
            continue;
        }

        const extension = input.dataset.extension
        allExtensions.push(extension)
    }

    let fromDate = null
    let toDate = null
    if (fromDateInput.value !== '') {
        fromDate = new Date(fromDateInput.value)
    }
    if (toDateInput.value !== '') {
        toDate = new Date(toDateInput.value)
    }

    console.log(fromDate)
    console.log(toDate)

    return {
        extensions: areAllChecked ? null : allExtensions,
        to: toDate,
        from: fromDate
    }
}